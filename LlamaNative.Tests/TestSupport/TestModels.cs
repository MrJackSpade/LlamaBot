using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace LlamaNative.Tests.TestSupport
{
    /// <summary>
    /// Resolves the GGUF model used by the end-to-end tests.
    ///
    /// Resolution order:
    /// 1. <c>dotnet user-secrets</c> key <c>TestModel:Path</c> (or <c>TestModelPath</c>) — a full path to a .gguf.
    ///    Set it with, from the repo root:
    ///    <code>dotnet user-secrets set "TestModel:Path" "D:\models\gemma-4-e4b.gguf" --project LlamaNative.Tests</code>
    /// 2. The <c>LLAMABOT_E2E_MODEL</c> environment variable (full path to a .gguf).
    /// 3. A cached copy of the tiny <c>stories260K.gguf</c> under <c>%TEMP%\llamabot-tests\</c>
    ///    (downloaded from Hugging Face on first use).
    /// Returns <c>null</c> if nothing could be resolved (e.g. no secret and offline) — callers should skip.
    ///
    /// GPU offload layers for the generation tests come from <c>TestModel:GpuLayers</c> (user secrets) or
    /// the <c>LLAMABOT_E2E_GPU_LAYERS</c> environment variable; default 0 (CPU only, universally safe).
    /// </summary>
    internal static class TestModels
    {
        private const string TinyModelFileName = "stories260K.gguf";

        private static readonly string[] TinyModelUrls =
        {
            "https://huggingface.co/ggml-org/models/resolve/main/tinyllamas/stories260K.gguf",
            "https://huggingface.co/ggml-org/models-moved/resolve/main/tinyllamas/stories260K.gguf",
        };

        private static readonly object _gate = new();
        private static IConfigurationRoot? _config;
        private static string? _resolvedPath;
        private static bool _attempted;

        private static IConfigurationRoot Config
        {
            get
            {
                lock (_gate)
                {
                    return _config ??= new ConfigurationBuilder()
                        .AddUserSecrets(typeof(TestModels).Assembly, optional: true)
                        .Build();
                }
            }
        }

        public static int GpuLayers
        {
            get
            {
                string? raw = Config["TestModel:GpuLayers"]
                              ?? Environment.GetEnvironmentVariable("LLAMABOT_E2E_GPU_LAYERS");
                return int.TryParse(raw, out int n) ? n : 0;
            }
        }

        public static string? GetTestModelPath()
        {
            lock (_gate)
            {
                if (_attempted)
                {
                    return _resolvedPath;
                }

                _attempted = true;
                _resolvedPath = Resolve();
                return _resolvedPath;
            }
        }

        private static string? Resolve()
        {
            string? fromSecret = Config["TestModel:Path"] ?? Config["TestModelPath"];
            if (!string.IsNullOrWhiteSpace(fromSecret) && File.Exists(fromSecret))
            {
                return fromSecret;
            }

            string? fromEnv = Environment.GetEnvironmentVariable("LLAMABOT_E2E_MODEL");
            if (!string.IsNullOrWhiteSpace(fromEnv) && File.Exists(fromEnv))
            {
                return fromEnv;
            }

            return TryGetCachedTinyModel();
        }

        private static string? TryGetCachedTinyModel()
        {
            string cacheDir = Path.Combine(Path.GetTempPath(), "llamabot-tests");
            string cachePath = Path.Combine(cacheDir, TinyModelFileName);

            if (File.Exists(cachePath) && new FileInfo(cachePath).Length > 0)
            {
                return cachePath;
            }

            try
            {
                Directory.CreateDirectory(cacheDir);

                using HttpClient http = new() { Timeout = TimeSpan.FromSeconds(45) };

                foreach (string url in TinyModelUrls)
                {
                    try
                    {
                        using HttpResponseMessage resp = http.GetAsync(url).GetAwaiter().GetResult();
                        if (!resp.IsSuccessStatusCode)
                        {
                            continue;
                        }

                        byte[] bytes = resp.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                        if (bytes.Length == 0)
                        {
                            continue;
                        }

                        string tmp = cachePath + ".part";
                        File.WriteAllBytes(tmp, bytes);
                        File.Move(tmp, cachePath, overwrite: true);
                        return cachePath;
                    }
                    catch
                    {
                        // try next mirror
                    }
                }
            }
            catch
            {
                // fall through -> null
            }

            return null;
        }
    }
}
