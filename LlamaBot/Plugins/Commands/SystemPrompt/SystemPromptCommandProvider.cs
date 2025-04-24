using LlamaBot.Extensions;
using LlamaBot.Plugins.Commands.ClearContext;
using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Interfaces;
using LlamaBot.Shared.Models;

namespace LlamaBot.Plugins.Commands.SystemPrompt
{
    internal class SystemPromptCommandProvider : ICommandProvider<SystemPromptCommand>
    {
        private const string PROMPTS_DIR = "SystemPrompts";
        private IDiscordService? _discordClient;
        private ILlamaBotClient? _llamaBotClient;
        private IPluginService? _pluginService;

        public string Command => "prompt";

        public string Description => "Updates or displays the bots system prompt";

        public SlashCommandOption[] SlashCommandOptions => [];

        public async Task<CommandResult> OnCommand(SystemPromptCommand command)
        {
            if (command.ClearContext)
            {
                await _pluginService!.Command(new ClearContextCommand(command.Command)
                {
                    IncludeCache = true,
                });
            }

            ulong channelId = command.Channel.GetChannelId();
            string responseString;

            if (command.Prompt is null)
            {
                if (!_llamaBotClient!.SystemPrompts.TryGetValue(channelId, out string? value))
                {
                    responseString = _llamaBotClient.DefaultSystemPrompt;
                }
                else
                {
                    responseString = value;
                }
            }
            else
            {
                string prompt = command.Prompt.Replace("\\n", "\n");
                _llamaBotClient!.SystemPrompts[channelId] = prompt;

                // Save the prompt to a file
                await this.SavePromptToFile(channelId, prompt);

                responseString = "System Prompt Updated: " + command.Prompt;
            }

            if (responseString.Length > 1995)
            {
                responseString = responseString[..1990] + "...";
            }

            return CommandResult.Success(responseString);
        }

        public async Task<InitializationResult> OnInitialize(InitializationEventArgs args)
        {
            _pluginService = args.PluginService;
            _discordClient = args.DiscordService;
            _llamaBotClient = args.LlamaBotClient;

            // Load all saved prompts
            await this.LoadAllPrompts();

            return InitializationResult.Success();
        }

        private async Task SavePromptToFile(ulong channelId, string prompt)
        {
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(PROMPTS_DIR);

                // Save the prompt to a file named after the channel ID
                string filePath = Path.Combine(PROMPTS_DIR, $"{channelId}.txt");
                await File.WriteAllTextAsync(filePath, prompt);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving prompt for channel {channelId}: {ex.Message}");
            }
        }

        private async Task LoadAllPrompts()
        {
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(PROMPTS_DIR);

                // Get all prompt files
                string[] promptFiles = Directory.GetFiles(PROMPTS_DIR, "*.txt");

                foreach (string file in promptFiles)
                {
                    // Extract channel ID from filename
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    if (ulong.TryParse(fileName, out ulong channelId))
                    {
                        // Read the prompt and add it to the dictionary
                        string prompt = await File.ReadAllTextAsync(file);
                        _llamaBotClient!.SystemPrompts[channelId] = prompt;
                    }
                }

                Console.WriteLine($"Loaded {promptFiles.Length} system prompts");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading system prompts: {ex.Message}");
            }
        }
    }
}