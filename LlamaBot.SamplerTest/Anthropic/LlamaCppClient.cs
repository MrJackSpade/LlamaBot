using LlamaBot.SamplerTest.Anthropic.Models;
using LlamaNative.Chat;
using LlamaNative.Sampling.Settings;
using LlamaNative.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;

namespace LlamaBot.SamplerTest.Anthropic
{
    /// <summary>
    /// IRemoteApi implementation that launches and communicates with llama.cpp server (ddh0 version).
    /// Uses adaptive-p sampling with specified target, decay, min-p, and top-k of 64.
    /// </summary>
    public class LlamaCppClient : IRemoteApi
    {
        private const string DefaultHost = "127.0.0.1";

        private const int DefaultPort = 8080;

        private const string ServerPath = @"d:\git\ddh0-llama.cpp\build\bin\Release\llama-server.exe";

        private readonly float _adaptivePDecay;

        private readonly float _adaptivePTarget;

        private readonly string _baseUrl;

        private readonly int _contextSize;

        private readonly int _gpuLayers;

        private readonly HttpClient _httpClient;

        private readonly int _kvCacheType;

        private readonly float _minP;

        private readonly string _modelPath;

        private readonly string? _tensorOverrides;

        private Process? _serverProcess;

        /// <summary>
        /// Creates a new LlamaCppClient.
        /// </summary>
        public LlamaCppClient(
            string modelPath,
            float adaptivePTarget = 0.6f,
            float adaptivePDecay = 0.9f,
            float minP = 0.03f,
            string? tensorOverrides = null,
            int contextSize = 32000,
            int gpuLayers = 100,
            int kvCacheType = 8,
            int port = DefaultPort)
        {
            _modelPath = modelPath;
            _adaptivePTarget = adaptivePTarget;
            _adaptivePDecay = adaptivePDecay;
            _minP = minP;
            _tensorOverrides = tensorOverrides;
            _contextSize = contextSize;
            _gpuLayers = gpuLayers;
            _kvCacheType = kvCacheType;
            _baseUrl = $"http://{DefaultHost}:{port}";
            _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        }

        /// <summary>
        /// Creates a LlamaCppClient from ChatSettings configuration.
        /// </summary>
        public static LlamaCppClient FromChatSettings(ChatSettings chatSettings, int port = DefaultPort)
        {
            // Get model settings
            string modelPath = chatSettings.ModelSettings?.ModelPath
                ?? throw new ArgumentException("ModelPath is required");
            int gpuLayers = chatSettings.ModelSettings?.GpuLayerCount ?? 100;
            string? tensorOverrides = chatSettings.ModelSettings?.TensorBufferTypeOverrides;

            // Get context settings
            int contextSize = (int)(chatSettings.ContextSettings.ContextSize ?? 32000);
            int kvCacheType = (int)chatSettings.ContextSettings.TypeK; // GgmlType enum value (8 = Q8_0)

            // Get sampler settings from default sampler set
            float adaptivePTarget = 0.6f;
            float adaptivePDecay = 0.9f;
            float minP = 0.03f;

            LlamaNative.Chat.Models.SamplerSetConfiguration? defaultSamplerSet = chatSettings.SamplerSets.FirstOrDefault();
            if (defaultSamplerSet?.TokenSelector != null)
            {
                object settings = defaultSamplerSet.TokenSelector.InstantiateSelectorSettings();

                if (settings is UnboundedQuadraticSamplerSettings uqSettings)
                {
                    adaptivePTarget = uqSettings.Target;
                    adaptivePDecay = uqSettings.TailDecay;
                    minP = uqSettings.MinP;
                }
                else if (settings is BaseDynamicSamplerSettings dynamicSettings)
                {
                    // Try to get MinP from base class
                    minP = dynamicSettings.MinP;

                    // Target might be on a derived type
                    if (settings is TargetedEntropySamplerSettings targetedSettings)
                    {
                        adaptivePTarget = targetedSettings.Target;
                    }
                }
            }

            return new LlamaCppClient(
                modelPath,
                adaptivePTarget,
                adaptivePDecay,
                minP,
                tensorOverrides,
                contextSize,
                gpuLayers,
                kvCacheType,
                port);
        }

        public void Dispose()
        {
            _httpClient.Dispose();

            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                try
                {
                    _serverProcess.Kill();
                    _serverProcess.WaitForExit(5000);
                }
                catch
                {
                    // Best effort cleanup
                }
                _serverProcess.Dispose();
            }
        }

        public async Task<AnthropicResponse> SendMessageAsync(
                    string systemPrompt,
                    List<AnthropicMessage> messages,
                    List<JObject>? tools = null)
        {
            // Build the request for llama.cpp /v1/chat/completions endpoint
            List<object> chatMessages = [];

            // Add system prompt
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                chatMessages.Add(new { role = "system", content = systemPrompt });
            }

            // Add conversation messages
            foreach (AnthropicMessage msg in messages)
            {
                string content = msg.GetTextContent();
                chatMessages.Add(new { role = msg.Role, content });
            }

            var requestBody = new
            {
                messages = chatMessages,
                max_tokens = 4096,
                temperature = 1.0,
                top_k = 64,
                min_p = _minP,
                stream = false
            };

            string requestJson = JsonConvert.SerializeObject(requestBody);
            StringContent content_post = new(requestJson, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync($"{_baseUrl}/v1/chat/completions", content_post);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"llama.cpp API error: {response.StatusCode}\n{responseBody}");
            }

            // Parse the OpenAI-compatible response and convert to AnthropicResponse format
            JObject jsonResponse = JObject.Parse(responseBody);
            string? assistantMessage = jsonResponse["choices"]?[0]?["message"]?["content"]?.ToString();

            AnthropicResponse anthropicResponse = new()
            {
                Id = jsonResponse["id"]?.ToString() ?? Guid.NewGuid().ToString(),
                Model = jsonResponse["model"]?.ToString() ?? "llama.cpp",
                Role = "assistant",
                Type = "message",
                Content =
                [
                    new AnthropicResponseContent
                    {
                        Type = "text",
                        Text = assistantMessage ?? string.Empty
                    }
                ]
            };

            return anthropicResponse;
        }

        /// <summary>
        /// Starts the llama.cpp server with the configured settings.
        /// </summary>
        public async Task StartServerAsync()
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                return; // Server already running
            }

            StringBuilder argsBuilder = new();
            argsBuilder.Append($"--model \"{_modelPath}\" ");
            argsBuilder.Append($"--host {DefaultHost} ");
            argsBuilder.Append($"--port {this.ExtractPort()} ");
            argsBuilder.Append($"--ctx-size {_contextSize} ");
            argsBuilder.Append($"--n-gpu-layers {_gpuLayers} ");
            argsBuilder.Append($"--flash-attn ");

            // KV cache quantization
            argsBuilder.Append($"--cache-type-k q{_kvCacheType}_0 ");
            argsBuilder.Append($"--cache-type-v q{_kvCacheType}_0 ");

            // Sampler settings
            argsBuilder.Append($"--top-k 64 ");
            argsBuilder.Append($"--min-p {_minP} ");
            argsBuilder.Append($"--adaptive-p-target {_adaptivePTarget} ");
            argsBuilder.Append($"--adaptive-p-decay {_adaptivePDecay} ");

            // MoE tensor overrides if specified
            if (!string.IsNullOrEmpty(_tensorOverrides))
            {
                argsBuilder.Append($"-ot \"{_tensorOverrides}\" ");
            }

            ProcessStartInfo startInfo = new()
            {
                FileName = ServerPath,
                Arguments = argsBuilder.ToString(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            Console.WriteLine($"Starting llama.cpp server: {ServerPath} {argsBuilder}");
            _serverProcess = Process.Start(startInfo);

            if (_serverProcess == null)
            {
                throw new InvalidOperationException("Failed to start llama.cpp server");
            }

            // Wait for server to be ready
            await this.WaitForServerReadyAsync();
            Console.WriteLine("llama.cpp server started successfully");
        }

        private int ExtractPort()
        {
            Uri uri = new(_baseUrl);
            return uri.Port;
        }

        private async Task WaitForServerReadyAsync(int maxWaitSeconds = 180)
        {
            DateTime deadline = DateTime.Now.AddSeconds(maxWaitSeconds);

            while (DateTime.Now < deadline)
            {
                try
                {
                    HttpResponseMessage response = await _httpClient.GetAsync($"{_baseUrl}/health");
                    if (response.IsSuccessStatusCode)
                    {
                        return;
                    }
                }
                catch
                {
                    // Server not ready yet
                }
                await Task.Delay(1000);
            }

            throw new TimeoutException($"llama.cpp server did not become ready within {maxWaitSeconds} seconds");
        }
    }
}