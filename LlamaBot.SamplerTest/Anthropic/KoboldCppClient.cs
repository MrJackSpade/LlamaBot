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
    /// IRemoteApi implementation that launches and communicates with KoboldCpp.
    /// Uses adaptive-p sampling with specified target, decay, min-p, and top-k of 64.
    /// </summary>
    public class KoboldCppClient : IRemoteApi
    {
        private const string DefaultHost = "127.0.0.1";

        private const int DefaultPort = 5001;

        private const string KoboldCppPath = @"d:\git\koboldcpp\koboldcpp.py";

        private readonly float _adaptivePDecay;

        private readonly float _adaptivePTarget;

        private readonly string _baseUrl;

        private readonly int _contextSize;

        private readonly int _gpuLayers;

        private readonly HttpClient _httpClient;

        private readonly int _kvCacheQuantLevel;

        private readonly float _minP;

        private readonly string _modelPath;

        private readonly string? _tensorOverrides;

        private Process? _serverProcess;

        /// <summary>
        /// Creates a new KoboldCppClient.
        /// </summary>
        public KoboldCppClient(
            string modelPath,
            float adaptivePTarget = 0.6f,
            float adaptivePDecay = 0.9f,
            float minP = 0.03f,
            string? tensorOverrides = null,
            int contextSize = 32000,
            int gpuLayers = 100,
            int kvCacheQuantLevel = 1,
            int port = DefaultPort)
        {
            _modelPath = modelPath;
            _adaptivePTarget = adaptivePTarget;
            _adaptivePDecay = adaptivePDecay;
            _minP = minP;
            _tensorOverrides = tensorOverrides;
            _contextSize = contextSize;
            _gpuLayers = gpuLayers;
            _kvCacheQuantLevel = kvCacheQuantLevel;
            _baseUrl = $"http://{DefaultHost}:{port}";
            _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        }

        /// <summary>
        /// Creates a KoboldCppClient from ChatSettings configuration.
        /// </summary>
        public static KoboldCppClient FromChatSettings(ChatSettings chatSettings, int port = DefaultPort)
        {
            // Get model settings
            string modelPath = chatSettings.ModelSettings?.ModelPath
                ?? throw new ArgumentException("ModelPath is required");
            int gpuLayers = chatSettings.ModelSettings?.GpuLayerCount ?? 100;
            string? tensorOverrides = chatSettings.ModelSettings?.TensorBufferTypeOverrides;

            // Get context settings
            int contextSize = (int)(chatSettings.ContextSettings.ContextSize ?? 32000);

            // Convert GgmlType to KoboldCpp quantkv level (0=F16, 1=Q8, 2=Q4)
            int kvCacheQuantLevel = (int)chatSettings.ContextSettings.TypeK switch
            {
                8 => 1,  // Q8_0 -> quantkv 1
                4 => 2,  // Q4_0 -> quantkv 2
                _ => 0   // F16 or other -> quantkv 0
            };

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

            return new KoboldCppClient(
                modelPath,
                adaptivePTarget,
                adaptivePDecay,
                minP,
                tensorOverrides,
                contextSize,
                gpuLayers,
                kvCacheQuantLevel,
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
            // Build the full prompt for KoboldCpp /api/v1/generate endpoint
            StringBuilder promptBuilder = new();

            // Add system prompt
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                promptBuilder.AppendLine($"System: {systemPrompt}");
                promptBuilder.AppendLine();
            }

            // Add conversation messages
            foreach (AnthropicMessage msg in messages)
            {
                string content = msg.GetTextContent();
                string role = msg.Role == "user" ? "Human" : "Assistant";
                promptBuilder.AppendLine($"{role}: {content}");
                promptBuilder.AppendLine();
            }

            // Add assistant prefix to prompt for continuation
            promptBuilder.Append("Assistant: ");

            var requestBody = new
            {
                prompt = promptBuilder.ToString(),
                max_length = 4096,
                max_context_length = _contextSize,
                temperature = 1.0f,
                top_k = 64,
                min_p = _minP,
                // Adaptive-p parameters for KoboldCpp
                adaptive_target = _adaptivePTarget,
                adaptive_decay = _adaptivePDecay,
                // Disable other samplers
                top_p = 1.0f,
                typical = 1.0f,
                tfs = 1.0f,
                top_a = 0.0f,
                rep_pen = 1.0f,
                sampler_order = new[] { 6, 0, 1, 3, 4, 2, 5 }, // Default order
                stop_sequence = new[] { "\nHuman:", "\nSystem:", "</s>" }
            };

            string requestJson = JsonConvert.SerializeObject(requestBody);
            StringContent content_post = new(requestJson, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync($"{_baseUrl}/api/v1/generate", content_post);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"KoboldCpp API error: {response.StatusCode}\n{responseBody}");
            }

            // Parse the KoboldCpp response and convert to AnthropicResponse format
            JObject jsonResponse = JObject.Parse(responseBody);
            JArray? results = jsonResponse["results"] as JArray;
            string? assistantMessage = results?[0]?["text"]?.ToString();

            AnthropicResponse anthropicResponse = new()
            {
                Id = Guid.NewGuid().ToString(),
                Model = "koboldcpp",
                Role = "assistant",
                Type = "message",
                Content =
                [
                    new AnthropicResponseContent
                    {
                        Type = "text",
                        Text = assistantMessage?.Trim() ?? string.Empty
                    }
                ]
            };

            return anthropicResponse;
        }

        /// <summary>
        /// Starts the KoboldCpp server with the configured settings.
        /// </summary>
        public async Task StartServerAsync()
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                return; // Server already running
            }

            StringBuilder argsBuilder = new();
            argsBuilder.Append($"\"{KoboldCppPath}\" ");
            argsBuilder.Append($"--model \"{_modelPath}\" ");
            argsBuilder.Append($"--host {DefaultHost} ");
            argsBuilder.Append($"--port {this.ExtractPort()} ");
            argsBuilder.Append($"--contextsize {_contextSize} ");
            argsBuilder.Append($"--gpulayers {_gpuLayers} ");
            argsBuilder.Append($"--flashattention ");
            argsBuilder.Append($"--usecublas ");

            // KV cache quantization (0=F16, 1=Q8, 2=Q4)
            argsBuilder.Append($"--quantkv {_kvCacheQuantLevel} ");

            // MoE tensor overrides if specified (same format as llama.cpp)
            if (!string.IsNullOrEmpty(_tensorOverrides))
            {
                argsBuilder.Append($"--overridetensors \"{_tensorOverrides}\" ");
            }

            ProcessStartInfo startInfo = new()
            {
                FileName = "python",
                Arguments = argsBuilder.ToString(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(KoboldCppPath) ?? @"d:\git\koboldcpp"
            };

            Console.WriteLine($"Starting KoboldCpp server: python {argsBuilder}");
            _serverProcess = Process.Start(startInfo);

            if (_serverProcess == null)
            {
                throw new InvalidOperationException("Failed to start KoboldCpp server");
            }

            // Wait for server to be ready
            await this.WaitForServerReadyAsync();
            Console.WriteLine("KoboldCpp server started successfully");
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
                    HttpResponseMessage response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/model");
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

            throw new TimeoutException($"KoboldCpp server did not become ready within {maxWaitSeconds} seconds");
        }
    }
}