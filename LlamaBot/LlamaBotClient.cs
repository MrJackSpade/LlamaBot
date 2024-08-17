using Discord;
using Discord.WebSocket;
using LlamaBot.Extensions;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Utils;
using LlamaNative.Chat;
using LlamaNative.Chat.Extensions;
using LlamaNative.Chat.Interfaces;
using LlamaNative.Chat.Models;
using LlamaNative.Sampling.Models;
using LlamaNative.Sampling.Samplers.Repetition;
using LlamaNative.Sampling.Settings;
using LlamaNative.Tokens.Models;
using Loxifi;
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
            Ensure.NotNull(character);
            Ensure.NotNull(character.ChatSettings);

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
                            SubSequence = _chatSettings.ChatTemplate.ToHeader(_chatSettings.BotName, false).Value
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
            Ensure.NotNull(_character);

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

        public async Task ProcessMessage(ISocketMessageChannel channel, bool continueLast)
        {
            Ensure.NotNull(_chatContext);

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

                    string displayName = this.GetDisplayName(historicalMessage.Author);

                    List<string> messageContent = [historicalMessage.Content];

                    if(_chatSettings.SplitSettings?.DoubleNewlineSplit ?? false)
                    {
                        messageContent = historicalMessage.Content.Split("\n").Select(s => s.Trim()).ToList();
                    }

                    TokenMask contentMask = historicalMessage.Author.Id == _botId ? 
                                                                TokenMask.Bot :
                                                                TokenMask.User;

                    foreach (string s in messageContent)
                    {
                        _chatContext.Insert(messageStart, contentMask, displayName, s);
                    }

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

            IMessage? prependMessage = null;

            if (continueLast)
            {
                prependMessage = await this.TryGetLastBotMessage(channel);
            }

            using IDisposable typingState = channel.EnterTypingState();

            foreach (ChatMessage cm in _chatContext.ReadResponse(continueLast))
            {
                string content = prependMessage?.Content ?? string.Empty;

                content += cm.Content;

                content = content.Trim();

                if (string.IsNullOrEmpty(content))
                {
                    await channel.SendMessageAsync("[Empty Message]");
                }
                else
                {
                    while (content.Length > 0)
                    {
                        int chunkSize = Math.Min(1950, content.Length);
                        string chunk = content[..chunkSize];
                        await channel.SendMessageAsync(chunk);
                        content = content[chunkSize..];
                    }
                }
            }

            if (prependMessage != null)
            {
                await prependMessage.DeleteAsync();
            }
        }

        public void SetClearDate(ulong channelId, DateTime triggered)
        {
            _metaData.ClearValues[channelId] = triggered;

            StaticConfiguration.Save(_metaData);
        }

        public async Task<IMessage?> TryGetLastBotMessage(ISocketMessageChannel channel)
        {
            await foreach (IMessage lm in channel.GetMessagesAsync(5).Flatten())
            {
                if (lm.Type == MessageType.ApplicationCommand)
                {
                    continue;
                }

                if (lm.Author.Id == _botId)
                {
                    return lm;
                }
                else
                {
                    return null;
                }
            }

            return null;
        }

        public void TryInterrupt()
        {
            Ensure.NotNull(_chatContext);
            _chatContext.TryInterrupt();
        }

        public void TryProcessMessageThread(ISocketMessageChannel smc, bool continueLast)
        {
            if (_processMessageThread is null || _processMessageThread.ThreadState != ThreadState.Running)
            {
                _processMessageThread = new Thread(async () => await this.ProcessMessage(smc, continueLast));
                _processMessageThread.Start();
            }
        }

        private void InsertContextHeaders()
        {
            Ensure.NotNull(_chatContext);
            Ensure.NotNull(_character);

            if (!string.IsNullOrWhiteSpace(SystemPrompt))
            {
                if (_chatSettings.SystemPromptUser is null)
                {
                    _chatContext.SendContent(TokenMask.Prompt, SystemPrompt);
                }
                else
                {
                    _chatContext.SendMessage(TokenMask.Prompt, _chatSettings.SystemPromptUser, SystemPrompt);
                }
            }

            foreach (CharacterMessage cm in _character.ChatMessages)
            {
                if(string.IsNullOrWhiteSpace(cm.Content))
                {
                    throw new ArgumentNullException("A message with null content was found in the configuration");
                }

                _chatContext.SendMessage(cm, string.Equals(cm.User, _character.ChatSettings.BotName, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}