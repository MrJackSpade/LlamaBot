using System.Runtime.CompilerServices;

namespace LlamaNative.Tests.TestSupport
{
    /// <summary>
    /// Ensures the CUDA Toolkit runtime DLLs (cudart64_*, cublas64_*, ...) are discoverable before any
    /// P/Invoke into the bundled llama.cpp libraries. CUDA 13.x keeps nvcc in <c>bin\</c> but the runtime
    /// DLLs in <c>bin\x64\</c>; the installer adds them to the machine PATH, but that may not be picked up
    /// by an already-running test host, so we (idempotently) prepend any we can find.
    /// </summary>
    internal static class NativeSetup
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            try
            {
                List<string> extra = new();

                foreach (string? root in new string?[]
                         {
                             Environment.GetEnvironmentVariable("CUDA_PATH"),
                             @"C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA"
                         })
                {
                    if (string.IsNullOrWhiteSpace(root))
                    {
                        continue;
                    }

                    // root may be a specific version dir (CUDA_PATH) or the parent dir
                    IEnumerable<string> versionDirs =
                        Directory.Exists(Path.Combine(root, "bin"))
                            ? new[] { root }
                            : Directory.Exists(root) ? Directory.GetDirectories(root) : Array.Empty<string>();

                    foreach (string v in versionDirs)
                    {
                        foreach (string sub in new[] { Path.Combine(v, "bin", "x64"), Path.Combine(v, "bin") })
                        {
                            if (Directory.Exists(sub))
                            {
                                extra.Add(sub);
                            }
                        }
                    }
                }

                if (extra.Count == 0)
                {
                    return;
                }

                string current = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                IEnumerable<string> toAdd = extra.Where(d => !current.Split(Path.PathSeparator).Contains(d, StringComparer.OrdinalIgnoreCase));
                string prefix = string.Join(Path.PathSeparator.ToString(), toAdd);

                if (prefix.Length > 0)
                {
                    Environment.SetEnvironmentVariable("PATH", prefix + Path.PathSeparator + current);
                }
            }
            catch
            {
                // Best-effort; if the CUDA bits aren't where we expect, the tests that need the native
                // libraries will surface a clear failure on their own.
            }
        }
    }
}
