using Discord;
using Discord.WebSocket;
using LlamaBot.Extensions;
using LlamaBot.Plugins.Interfaces;
using LlamaNative.Chat;
using LlamaNative.Chat.Extensions;
using LlamaNative.Chat.Interfaces;
using LlamaNative.Chat.Models;
using LlamaNative.Sampling.Models;
using LlamaNative.Sampling.Samplers.Repetition;
using LlamaNative.Sampling.Settings;
using Loxifi;
using System.Diagnostics;
using ThreadState = System.Threading.ThreadState;

namespace LlamaBot
{
    internal class LlamaBotClient : ILlamaBotClient
    {
        private readonly ulong _botId;

        private readonly Character? _character;

        private readonly IChatContext? _chatContext;

        private readonly ChatSettings _chatSettings;

        private readonly MetaData _metaData = StaticConfiguration.Load<MetaData>();

        private Thread _processMessageThread;

        public LlamaBotClient(Character character, string? systemPrompt, ulong botId)
        {
            _chatContext = LlamaChatClient.LoadChatContext(character.ChatSettings);

            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                SystemPrompt = systemPrompt;
            }

            _chatSettings = character.ChatSettings;
            _character = character;
            _botId = botId;

            if (_chatSettings.ResponseStartBlock > 0)
            {
                _chatSettings.SimpleSamplers.Add(
                    new SamplerSetting(
                        nameof(SubsequenceBlockingSampler),
                        new SubsequenceBlockingSamplerSettings()
                        {
                            ResponseStartBlock = _chatSettings.ResponseStartBlock,
                            SubSequence = _chatSettings.ChatTemplate.ToHeader(_chatSettings.BotName, false)
                        }
                    ));
            }
        }

        public string SystemPrompt { get; set; } = string.Empty;

        public void Clear(bool v)
        {
            _chatContext?.Clear(v);
        }

        public string GetDisplayName(IUser user)
        {
            if (user.Id == _botId)
            {
                return _chatSettings.BotName;
            }

            if (_character.NameOverride.TryGetValue(user.Username, out string? name))
            {
                return name;
            }

            if (user is IGuildUser guildUser)
            {
                return guildUser.DisplayName;
            }

            return user.Username;
        }

        public async Task ProcessMessage(ISocketMessageChannel channel)
        {
            Console.Clear();

            _chatContext.Clear(false);

            this.InsertContextHeaders();

            int messageStart = _chatContext.MessageCount;

            DateTime stop = DateTime.MinValue;

            if (_metaData.ClearValues.TryGetValue(channel.Id, out DateTime savedStop))
            {
                stop = savedStop;
            }

            await foreach (IReadOnlyCollection<IMessage>? historicalMessages in channel.GetMessagesAsync(1000))
            {
                bool done = false;

                foreach (IMessage historicalMessage in historicalMessages)
                {
                    if (historicalMessage.CreatedAt.DateTime < stop)
                    {
                        done = true;
                        break;
                    }

                    if (historicalMessage.Type == MessageType.ApplicationCommand)
                    {
                        continue;
                    }

                    _chatContext.Insert(messageStart, this.GetDisplayName(historicalMessage.Author), historicalMessage.Content, historicalMessage.Id);

                    if (_chatContext.AvailableBuffer < 1000)
                    {
                        done = true;
                        break;
                    }
                }

                if (done)
                {
                    break;
                }
            }

            //string nextUser = _chatContext.PredictNextUser();

            //Debug.WriteLine("Predicted User:" + nextUser);

            using IDisposable typingState = channel.EnterTypingState();

            foreach (ChatMessage cm in _chatContext.ReadResponse())
            {
                if (string.IsNullOrEmpty(cm.Content))
                {
                    await channel.SendMessageAsync("[Empty Message]");
                }
                else
                {
                    string content = cm.Content;
                    while (content.Length > 0)
                    {
                        int chunkSize = Math.Min(1950, content.Length);
                        string chunk = content[..chunkSize];
                        await channel.SendMessageAsync(chunk);
                        content = content[chunkSize..];
                    }
                }
            }
        }

        public void SetClearDate(ulong channelId, DateTime triggered)
        {
            _metaData.ClearValues[channelId] = triggered;

            StaticConfiguration.Save(_metaData);
        }

        public void TryInterrupt()
        {
            _chatContext.TryInterrupt();
        }

        public void TryProcessMessageThread(ISocketMessageChannel smc)
        {
            if (_processMessageThread is null || _processMessageThread.ThreadState != ThreadState.Running)
            {
                _processMessageThread = new Thread(async () => await this.ProcessMessage(smc));
                _processMessageThread.Start();
            }
        }

        private void InsertContextHeaders()
        {
            if (!string.IsNullOrWhiteSpace(SystemPrompt))
            {
                _chatContext.SendMessage(_chatSettings.SystemPromptUser, SystemPrompt);
            }

            foreach (ChatMessage cm in _character.ChatMessages)
            {
                _chatContext.SendMessage(cm);
            }
        }
    }
}