using LlamaBot.Extensions;
using LlamaBot.Plugins.Commands.ClearContext;
using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Interfaces;
using LlamaBot.Shared.Models;

namespace LlamaBot.Plugins.Commands.Think
{
    internal class ThinkCommandCommandProvider : ICommandProvider<ThinkCommand>
    {
        private const string THOUGHTS_DIR = "Thoughts";
        private IDiscordService? _discordClient;
        private ILlamaBotClient? _llamaBotClient;
        private IPluginService? _pluginService;

        public string Command => "think";

        public string Description => "Updates or displays the bots forced thoughts";

        public SlashCommandOption[] SlashCommandOptions => [];

        public async Task<CommandResult> OnCommand(ThinkCommand command)
        {
            ulong channelId = command.Channel.GetChannelId();

            string? responseString;

            if (command.Think is null)
            {
                if (!_llamaBotClient!.ChannelSettings.TryGetValue(channelId, out ChannelSettings? value) || string.IsNullOrWhiteSpace(value.Think))
                {
                    responseString = _llamaBotClient.DefaultChannelSettings.Think;
                }
                else
                {
                    responseString = value.Think;
                }
            }
            else
            {
                string prompt = command.Think.Replace("\\n", "\n");
                _llamaBotClient!.ChannelSettings[channelId].Think = prompt;

                // Save the prompt to a file
                await SavePromptToFile(channelId, prompt);

                responseString = "Thoughts Updated: " + command.Think;
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
            await LoadAllPrompts();

            return InitializationResult.Success();
        }

        private async Task SavePromptToFile(ulong channelId, string prompt)
        {
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(THOUGHTS_DIR);

                // Save the prompt to a file named after the channel ID
                string filePath = Path.Combine(THOUGHTS_DIR, $"{channelId}.txt");
                await File.WriteAllTextAsync(filePath, prompt);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error thoughts for channel {channelId}: {ex.Message}");
            }
        }

        private async Task LoadAllPrompts()
        {
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(THOUGHTS_DIR);

                // Get all think files
                string[] thoughtFiles = Directory.GetFiles(THOUGHTS_DIR, "*.txt");

                foreach (string file in thoughtFiles)
                {
                    // Extract channel ID from filename
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    if (ulong.TryParse(fileName, out ulong channelId))
                    {
                        // Read the think and add it to the dictionary
                        string think = await File.ReadAllTextAsync(file);

                        if (!_llamaBotClient!.ChannelSettings.TryGetValue(channelId, out var channelSettings))
                        {
                            channelSettings = new ChannelSettings();
                            _llamaBotClient.ChannelSettings.Add(channelId, channelSettings);
                        }

                        channelSettings.Think = think;
                    }
                }

                Console.WriteLine($"Loaded {thoughtFiles.Length} thoughts");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading thoughts: {ex.Message}");
            }
        }
    }
}