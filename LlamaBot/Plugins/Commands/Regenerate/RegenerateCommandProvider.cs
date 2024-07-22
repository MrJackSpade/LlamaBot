using Discord;
using Discord.WebSocket;
using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Models;

namespace LlamaBot.Plugins.Commands.Regenerate
{
    internal class RegenerateCommandProvider : ICommandProvider<RegenerateCommand>
    {
        public string Command => "regenerate";

        public string Description => "Regenerates the last bot message";

        public SlashCommandOption[] SlashCommandOptions => [];

        public async Task<CommandResult> OnCommand(RegenerateCommand command)
        {
            List<IMessage> messages = [];

            if (command.Channel is ISocketMessageChannel smc)
            {
                foreach (IMessage checkMessage in await smc.GetMessagesAsync(500).FlattenAsync())
                {
                    if (checkMessage.Type == MessageType.ApplicationCommand)
                    {
                        continue;
                    }

                    if (checkMessage.Author.Id == _discordClient.CurrentUser.Id)
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

                await OnContinueCommand(command);
                return CommandResult.Success();
            }
            else
            {
                return CommandResult.Error("No message found after checking 500 messages");
            }
        }

        public Task<InitializationResult> OnInitialize(InitializationEventArgs args)
        {
            throw new NotImplementedException();
        }
    }
}