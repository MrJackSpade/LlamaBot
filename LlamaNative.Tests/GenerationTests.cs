using LlamaNative;
using LlamaNative.Apis;
using LlamaNative.Interfaces;
using LlamaNative.Interop.Apis;
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
    /// End-to-end generation against a real (tiny) GGUF model: load → context → feed a prompt → decode →
    /// greedily sample N tokens → detokenize. Verifies the full native pipeline produces output and that
    /// greedy sampling is deterministic.
    /// </summary>
    public class GenerationTests(ITestOutputHelper output)
    {
        private const string Prompt = "Once upon a time";
        private const int GenerateTokenCount = 16;

        private static (string Text, int[] TokenIds) Generate(string modelPath)
        {
            using Model model = LlamaClient.LoadModel(new ModelSettings
            {
                ModelPath = modelPath,
                UseMemoryLock = false,
                GpuLayerCount = TestModels.GpuLayers,
            });

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

            INativeContext context = LlamaClient.LoadContext(
                model,
                contextSettings,
                [new SamplerSet { TokenSelector = new GreedySampler() }]);

            try
            {
                foreach (int id in NativeApi.Tokenize(model.Handle, Prompt, add_bos: true))
                {
                    context.Write(new Token(id, model.Handle.TokenToPiece(id), TokenMask.Undefined));
                }

                context.Evaluate();

                List<int> generated = new();

                for (int i = 0; i < GenerateTokenCount; i++)
                {
                    Token next = context.SelectToken(null, new GreedySamplerSettings(), out _);

                    if (next == Token.Null)
                    {
                        break;
                    }

                    generated.Add(next.Id);
                    context.Write(next);
                    context.Evaluate();
                }

                string text = string.Concat(generated.Select(id => model.Handle.TokenToPiece(id)));
                return (text, generated.ToArray());
            }
            finally
            {
                context.Dispose();
            }
        }

        [SkippableFact]
        public void Generation_ProducesNonEmptyOutput()
        {
            string? path = TestModels.GetTestModelPath();
            Skip.If(path is null, "No test model available (set user-secret TestModel:Path, or LLAMABOT_E2E_MODEL, or allow the stories260K download).");

            (string text, int[] ids) = Generate(path!);

            output.WriteLine($"Generated {ids.Length} token(s): \"{text}\"");

            Assert.NotEmpty(ids);
            Assert.False(string.IsNullOrWhiteSpace(text), "Generated text should not be empty/whitespace.");
        }

        [SkippableFact]
        public void GreedyGeneration_IsDeterministic()
        {
            string? path = TestModels.GetTestModelPath();
            Skip.If(path is null, "No test model available (set user-secret TestModel:Path, or LLAMABOT_E2E_MODEL, or allow the stories260K download).");

            (string textA, int[] idsA) = Generate(path!);
            (string textB, int[] idsB) = Generate(path!);

            output.WriteLine($"Run A: \"{textA}\"");
            output.WriteLine($"Run B: \"{textB}\"");

            Assert.Equal(idsA, idsB);
            Assert.Equal(textA, textB);
        }
    }
}
