using Discord.WebSocket;
using LlamaNative.Chat;
using LlamaNative.Chat.Interfaces;
using LlamaNative.Chat.Models;
using LlamaNative.Utils;
using Loxifi;

namespace LlamaBot
{
    internal class Program
    {
        private static readonly Configuration configuration = StaticConfiguration.Load<Configuration>();

        private static IChatContext chatContext;

        private static RecursiveConfigurationReader<CharacterConfiguration> recursiveConfigurationReader = new("Characters");

        public static async Task MessageReceived(SocketMessage message)
        {
            if (message.Author.IsBot)
            {
                return;
            }

            if (message.Channel is not SocketTextChannel socketTextChannel)
            {
                return;
            }

            if (!configuration.ChannelIds.Contains(socketTextChannel.Id))
            {
                return;
            }

            ChatMessage chatMessage = new(message.Author.Username, message.Content);

            chatContext.SendMessage(chatMessage);

            using IDisposable typingState = message.Channel.EnterTypingState();

            ChatMessage response = chatContext.ReadResponse();

            chatContext.SendMessage(response);      

            await message.Channel.SendMessageAsync(response.Content);
        }

        private static async Task Main(string[] args)
        {
            RecursiveConfiguration<CharacterConfiguration> recursiveConfiguration = recursiveConfigurationReader.Read("LlamaBot");

            CharacterConfiguration characterConfiguration = recursiveConfiguration.Configuration;

            chatContext = LlamaChatClient.LoadChatContext(characterConfiguration.ChatSettings);

            if (recursiveConfiguration.Resources.TryGetValue("System.txt", out string? systemText))
            {
                chatContext.SendMessage("System", systemText);
            }

            DiscordClient discordClient = new(configuration.DiscordToken);

            Console.WriteLine("Connecting to Discord...");

            await discordClient.Connect();

            Console.WriteLine("Connected.");

            discordClient.MessageReceived += MessageReceived;

            await Task.Delay(-1);
        }
    }
}