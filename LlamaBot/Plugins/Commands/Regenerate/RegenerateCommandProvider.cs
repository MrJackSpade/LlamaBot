using Discord;
using Discord.WebSocket;
using LlamaBot.Plugins.Commands.Generate;
using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Models;
using LlamaNative.Chat.Models;

namespace LlamaBot.Plugins.Commands.Regenerate
{
    internal class RegenerateCommandProvider : ICommandProvider<RegenerateCommand>
    {
        private ILlamaBotClient? _llamaBotClient;

        private IPluginService? _pluginService;

        public string Command => "regenerate";

        public string Description => "Regenerates the last bot message";

        public SlashCommandOption[] SlashCommandOptions => [];

        public async Task<CommandResult> OnCommand(RegenerateCommand command)
        {
            List<IMessage> messages = [];
            string? regenerateUser = null;

            if (command.Channel is ISocketMessageChannel smc)
            {
                IMessage? rootMessage = await _llamaBotClient.TryGetLastBotMessage(smc);

                if (rootMessage is null)
                {
                    return CommandResult.Error("No bot message found");
                }

                ParsedMessage parsedRootMessage = _llamaBotClient.ParseMessage(rootMessage);

                regenerateUser = parsedRootMessage.Author;

                foreach (IMessage checkMessage in await smc.GetMessagesAsync(500).FlattenAsync())
                {
                    if (checkMessage.Type == MessageType.ApplicationCommand)
                    {
                        continue;
                    }

                    ParsedMessage parsed = _llamaBotClient.ParseMessage(checkMessage);

                    if (parsed.Author == regenerateUser)
                    {
                        messages.Add(checkMessage);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (messages.Count > 0)
            {
                foreach (IMessage message in messages)
                {
                    await message.DeleteAsync();
                }

                await _pluginService.Command(new GenerateCommand(command.Command)
                {
                    UserName = regenerateUser
                });

                return CommandResult.Success();
            }
            else
            {
                return CommandResult.Error("No message found after checking 500 messages");
            }
        }

        public Task<InitializationResult> OnInitialize(InitializationEventArgs args)
        {
            _pluginService = args.PluginService;
            _llamaBotClient = args.LlamaBotClient;
            return InitializationResult.SuccessAsync();
        }
    }
}