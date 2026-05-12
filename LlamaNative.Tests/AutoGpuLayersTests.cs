using LlamaNative;
using LlamaNative.Apis;
using LlamaNative.Interfaces;
using LlamaNative.Interop.Settings;
using LlamaNative.Interop.Structs;
using LlamaNative.Models;
using LlamaNative.Sampling.Models;
using LlamaNative.Sampling.Samplers.Temperature;
using LlamaNative.Tests.TestSupport;
using LlamaNative.Tokens.Models;
using Xunit;
using Xunit.Abstractions;

namespace LlamaNative.Tests
{
    /// <summary>
    /// Exercises the automatic GPU-layer fitting path (<see cref="ModelSettings.AutoGpuLayerCount"/>): the model should
    /// load and generate, and the context size handling should behave as documented (a declared context size is left
    /// untouched; an undeclared one is populated from what the fitter chose).
    /// </summary>
    public class AutoGpuLayersTests(ITestOutputHelper output)
    {
        [SkippableFact]
        public void AutoFit_LoadsAndGenerates_WithDeclaredContextSize()
        {
            string? path = TestModels.GetTestModelPath();
            Skip.If(path is null, "No test model available (set user-secret TestModel:Path, or LLAMABOT_E2E_MODEL, or allow the stories260K download).");

            ContextSettings contextSettings = new()
            {
                ContextSize = 256,
                BatchSize = 256,
                ThreadCount = 2,
                OffloadKQV = false,
                FlashAttentionType = FlashAttentionType.Disabled,
                TypeK = GgmlType.GGML_TYPE_F16,
                TypeV = GgmlType.GGML_TYPE_F16,
            };

            using Model model = LlamaClient.LoadModel(
                new ModelSettings
                {
                    ModelPath = path!,
                    UseMemoryLock = false,
                    GpuLayerCount = ModelSettings.AutoGpuLayerCount,
                },
                contextSettings);

            // A declared context size must not be reduced by the fitter (there is no FitMinContextSize).
            Assert.Equal(256u, contextSettings.ContextSize);

            INativeContext context = LlamaClient.LoadContext(
                model,
                contextSettings,
                [new SamplerSet { TokenSelector = new GreedySampler() }]);

            try
            {
                foreach (int id in NativeApi.Tokenize(model.Handle, "Once upon a time", add_bos: true))
                {
                    context.Write(new Token(id, model.Handle.TokenToPiece(id), TokenMask.Undefined));
                }

                context.Evaluate();

                Token next = context.SelectToken(null, new GreedySamplerSettings(), out _);
                output.WriteLine($"First generated token: {next.Id} (\"{model.Handle.TokenToPiece(next.Id)}\")");

                Assert.NotEqual(Token.Null, next);
            }
            finally
            {
                context.Dispose();
            }
        }

        [SkippableFact]
        public void AutoFit_PopulatesContextSize_WhenNotDeclared()
        {
            string? path = TestModels.GetTestModelPath();
            Skip.If(path is null, "No test model available (set user-secret TestModel:Path, or LLAMABOT_E2E_MODEL, or allow the stories260K download).");

            ContextSettings contextSettings = new()
            {
                ContextSize = null,
                ThreadCount = 2,
                OffloadKQV = false,
                FlashAttentionType = FlashAttentionType.Disabled,
            };

            using Model model = LlamaClient.LoadModel(
                new ModelSettings
                {
                    ModelPath = path!,
                    VocabOnly = false,
                    UseMemoryLock = false,
                    GpuLayerCount = ModelSettings.AutoGpuLayerCount,
                },
                contextSettings);

            output.WriteLine($"Fitter chose context size: {contextSettings.ContextSize}");

            // With no declared size the fitter picks one (the model's trained context, or smaller if it had to shrink).
            Assert.True(contextSettings.ContextSize is > 0, "Auto-fit should populate ContextSize when it wasn't declared.");
        }
    }
}
