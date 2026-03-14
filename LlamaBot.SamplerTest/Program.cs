using LlamaBot.SamplerTest.Anthropic;
using LlamaBot.SamplerTest.Extensions;
using LlamaBot.SamplerTest.Orchestration;
using LlamaBot.SamplerTest.Results;
using LlamaNative.Chat;
using LlamaNative.Chat.Interfaces;
using LlamaNative.Chat.Models;
using LlamaNative.Serialization;
using LlamaNative.Utils;
using Loxifi;
using Microsoft.Extensions.Configuration;

namespace LlamaBot.SamplerTest
{
    internal class Program
    {
        private static readonly Configuration _configuration;

        private static readonly RecursiveConfigurationReader<Character> _recursiveConfigurationReader = new("Characters");

        static Program()
        {
            _configuration = StaticConfiguration.Load<Configuration>();
        }

        private static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: LlamaBot.SamplerTest <character-name>");
                Console.WriteLine();
                Console.WriteLine("Available characters:");
                foreach (string config in _recursiveConfigurationReader.Configurations)
                {
                    Console.WriteLine($"  - {config}");
                }

                return;
            }

            string characterName = args[0];
            Console.WriteLine($"Loading character configuration: {characterName}");

            // Load character configuration
            RecursiveConfiguration<Character> recursiveConfiguration = _recursiveConfigurationReader.Read(characterName);
            Character character = recursiveConfiguration.Configuration;

            if (character.ChatSettings == null)
            {
                Console.WriteLine("Error: No ChatSettings found in character configuration");
                return;
            }

            // Load user secrets for Anthropic API key
            IConfigurationBuilder secretsBuilder = new ConfigurationBuilder()
                .AddUserSecrets<Program>();
            IConfigurationRoot secretsConfig = secretsBuilder.Build();

            UserSecrets userSecrets = new()
            {
                AnthropicApiKey = secretsConfig["AnthropicApiKey"] ?? string.Empty
            };

            if (string.IsNullOrEmpty(userSecrets.AnthropicApiKey))
            {
                Console.WriteLine("Error: AnthropicApiKey not found in user secrets");
                Console.WriteLine("Please set it using: dotnet user-secrets set \"AnthropicApiKey\" \"your-api-key\"");
                return;
            }

            Console.WriteLine("Anthropic API key loaded from user secrets");

            // Load the local model
            string modelPath = character.ChatSettings.ModelSettings?.ModelPath ?? "Unknown";
            string modelInfo = Path.GetFileNameWithoutExtension(modelPath);
            Console.WriteLine($"Loading model: {modelInfo}");

            IChatContext chatContext = LlamaChatClient.LoadChatContext(character.ChatSettings);
            Console.WriteLine("Model loaded successfully");

            string botName = character.ChatSettings.BotName;

            // Extract default sampler settings from configuration
            SamplerSetConfiguration defaultSamplerSet = character.ChatSettings.SamplerSets.GetDefault()
                ?? throw new ArgumentException("No default sampler set found");

            object defaultSamplerSettings;
            if (defaultSamplerSet.TokenSelector != null)
            {
                defaultSamplerSettings = defaultSamplerSet.TokenSelector.InstantiateSelectorSettings();
            }
            else
            {
                throw new ArgumentException("No TokenSelector configured in default sampler set");
            }

            Console.WriteLine($"Using sampler: {defaultSamplerSet.TokenSelector?.Type}");

            // Create Anthropic client
            using AnthropicClient anthropicClient = new(userSecrets.AnthropicApiKey);

            // Create orchestrator
            ConversationOrchestrator orchestrator = new(
                anthropicClient,
                chatContext,
                _configuration.MessageCount,
                modelInfo,
                botName,
                defaultSamplerSettings);

            // Run the conversation
            Console.WriteLine();
            Console.WriteLine("Starting sampler test...");
            Console.WriteLine();

            ConversationResult result = await orchestrator.RunConversationAsync();

            // Save results
            string outputDir = Path.IsPathRooted(_configuration.OutputDirectory)
                ? _configuration.OutputDirectory
                : Path.Combine(AppContext.BaseDirectory, _configuration.OutputDirectory);

            string fileName = ResultsWriter.GenerateFileName(modelInfo);
            string outputPath = Path.Combine(outputDir, fileName);

            await ResultsWriter.SaveConversationAsync(result, outputPath);

            Console.WriteLine();
            Console.WriteLine("Done!");
        }
    }
}