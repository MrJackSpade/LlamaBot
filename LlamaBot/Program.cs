using Discord;
using Discord.WebSocket;
using LlamaBot.Extensions;
using LlamaNative.Chat;
using LlamaNative.Chat.Extensions;
using LlamaNative.Chat.Interfaces;
using LlamaNative.Chat.Models;
using LlamaNative.Utils;
using Loxifi;
using System.Diagnostics;

namespace LlamaBot
{
    internal class Program
    {
        private static readonly Configuration _configuration = StaticConfiguration.Load<Configuration>();

        private static IChatContext _chatContext;

        private static DiscordClient _discordClient;

        private static RecursiveConfiguration<CharacterConfiguration> _recursiveConfiguration;

        private static readonly RecursiveConfigurationReader<CharacterConfiguration> _recursiveConfigurationReader = new("Characters");

        private static readonly DateTime _startTime = DateTime.Now;

        private static CharacterConfiguration _characterConfiguration => _recursiveConfiguration?.Configuration;

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

            if (!_configuration.ChannelIds.Contains(socketTextChannel.Id))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(message.Content))
            {
                return;
            }

            await ProcessMessage(message);
        }

        public static async Task ProcessMessage(SocketMessage message)
        {
            _chatContext.Clear();

            _chatContext.Insert(0, message.Author.Username, message.Content, message.Id);

            foreach (IMessage? historicalMessage in await message.Channel.GetMessagesAsync(message, Direction.Before, 100).FlattenAsync())
            {
                string displayName = historicalMessage.Author.Username;

                if (historicalMessage.Author.Id == _discordClient.CurrentUser.Id)
                {
                    displayName = _configuration.Character;
                }

                _chatContext.Insert(0, displayName, historicalMessage.Content, historicalMessage.Id);

                if (_chatContext.AvailableBuffer < 1000)
                {
                    break;
                }
            }

            InsertContextHeaders();

            //string userPrediction = _chatContext.PredictNextUser();

            //Debug.WriteLine($"Next Predicted Character: {userPrediction}");

            //if (userPrediction == _configuration.Character)
            //{
            using IDisposable typingState = message.Channel.EnterTypingState();

            ChatMessage response = _chatContext.ReadResponse();

            await message.Channel.SendMessageAsync(response.Content);
            //}
        }

        private static void InsertContextHeaders()
        {
            //_chatContext.Insert(0, _characterConfiguration.ChatSettings.BotName, $"I am {_characterConfiguration.ChatSettings.BotName}. Here to assist you with your chat needs.");

            if (_recursiveConfiguration.Resources.TryGetValue("System.txt", out string? systemText))
            {
                _chatContext.Insert(0, "System", systemText);
            }
        }

        private static async Task Main(string[] args)
        {
            _recursiveConfiguration = _recursiveConfigurationReader.Read("Miyako");

            _chatContext = LlamaChatClient.LoadChatContext(_characterConfiguration.ChatSettings);

            _discordClient = new(_configuration.DiscordToken);

            Console.WriteLine("Connecting to Discord...");

            await _discordClient.Connect();

            Console.WriteLine("Connected.");

            _discordClient.MessageReceived += MessageReceived;

            await Task.Delay(-1);
        }
    }
}