using LlamaNative;
using LlamaNative.Apis;
using LlamaNative.Interop.Apis;
using LlamaNative.Interop.Settings;
using LlamaNative.Models;
using LlamaNative.Tests.TestSupport;
using Xunit;

namespace LlamaNative.Tests
{
    /// <summary>
    /// Exercises the lowest-friction slice of the native interop: load a model (vocab only — no weights
    /// needed, so this runs even without GPU/large models), read the vocab size, tokenize and detokenize.
    /// This is the path that breaks most often when llama.cpp is bumped.
    /// </summary>
    public class InteropTests
    {
        private const string SampleText = "The quick brown fox jumps over the lazy dog.";

        [SkippableFact]
        public void VocabOnlyModel_LoadsAndExposesVocab()
        {
            string? path = TestModels.GetTestModelPath();
            Skip.If(path is null, "No test model available (set user-secret TestModel:Path, or LLAMABOT_E2E_MODEL, or allow the stories260K download).");

            using Model model = LlamaClient.LoadModel(new ModelSettings
            {
                ModelPath = path!,
                VocabOnly = true,
                UseMemoryLock = false,
                GpuLayerCount = 0,
            });

            Assert.True(model.NVocab > 0, "Vocabulary size should be positive.");
            Assert.Equal(model.NVocab, NativeApi.NVocab(model.Handle));
        }

        [SkippableFact]
        public void Tokenize_Then_Detokenize_RoundTrips()
        {
            string? path = TestModels.GetTestModelPath();
            Skip.If(path is null, "No test model available (set user-secret TestModel:Path, or LLAMABOT_E2E_MODEL, or allow the stories260K download).");

            using Model model = LlamaClient.LoadModel(new ModelSettings
            {
                ModelPath = path!,
                VocabOnly = true,
                UseMemoryLock = false,
                GpuLayerCount = 0,
            });

            int[] ids = NativeApi.Tokenize(model.Handle, SampleText, add_bos: false);
            Assert.NotEmpty(ids);
            Assert.All(ids, id => Assert.InRange(id, 0, model.NVocab - 1));

            string detokenized = string.Concat(ids.Select(id => model.Handle.TokenToPiece(id, special: false)));

            // Tokenizers may add a leading space and there can be other minor normalisation, but the
            // characters of the input (modulo whitespace/case) must come back.
            static string Normalize(string s) => new(s.Where(c => !char.IsWhiteSpace(c)).ToArray());

            Assert.Contains(Normalize(SampleText).ToLowerInvariant(),
                            Normalize(detokenized).ToLowerInvariant());
        }
    }
}
