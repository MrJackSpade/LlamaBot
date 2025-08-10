using Discord;
using Discord.Rest;
using Discord.WebSocket;
using LlamaBot.Extensions;
using LlamaBot.Plugins.Interfaces;
using LlamaNative.Chat;
using LlamaNative.Chat.Exceptions;
using LlamaNative.Chat.Extensions;
using LlamaNative.Chat.Interfaces;
using LlamaNative.Chat.Models;
using LlamaNative.Sampling.Models;
using LlamaNative.Sampling.Samplers.Repetition;
using LlamaNative.Sampling.Settings;
using LlamaNative.Tokens.Models;
using LlamaNative.Utils;
using Loxifi;
using ThreadState = System.Threading.ThreadState;

namespace LlamaBot
{
    internal class LlamaBotClient : ILlamaBotClient
    {
        private const char ZERO_WIDTH = (char)8203;

        private readonly ulong _botId;

        private readonly Character? _character;

        private readonly IChatContext? _chatContext;

        private readonly ChatSettings _chatSettings;

        private readonly MetaData _metaData = StaticConfiguration.Load<MetaData>();

        private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

        private Thread _processMessageThread;

        public string BotName => _chatSettings.BotName;

        public string DefaultSystemPrompt { get; } = string.Empty;

        public Dictionary<ulong, string> SystemPrompts { get; set; } = [];

        public LlamaBotClient(Character character, string? systemPrompt, ulong botId)
        {
            Ensure.NotNull(character);
            Ensure.NotNull(character.ChatSettings);

            _chatContext = LlamaChatClient.LoadChatContext(character.ChatSettings);

            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                DefaultSystemPrompt = systemPrompt;
            }

            _chatSettings = character.ChatSettings;
            _character = character;
            _botId = botId;

            if (_chatSettings.ResponseStartBlock > 0)
            {
                SamplerSetConfiguration defaultSet = _chatSettings.SamplerSets.GetDefault()
                                                     ?? throw new ArgumentException("No default sampler set found");

                defaultSet.SimpleSamplers.Add(
                    new SamplerSetting(
                        nameof(SubsequenceBlockingSampler),
                        new SubsequenceBlockingSamplerSettings()
                        {
                            ResponseStartBlock = _chatSettings.ResponseStartBlock,
                            SubSequences = [_chatSettings.ChatTemplate.ToHeader(_chatSettings.BotName, false, HeaderType.Assistant).Value]
                        }
                    ));
            }
        }

        public string BuildMessage(string author, string content)
        {
            string header = string.Empty;

            if (author != _chatSettings.BotName)
            {
                header += $"{ZERO_WIDTH}**{author}:**{ZERO_WIDTH}";
            }

            return header + content;
        }

        public void Clear(bool v)
        {
            _chatContext?.Clear(v);
        }

        public async Task<string?> GenerateMessageBody(ISocketMessageChannel channel, string user)
        {
            Ensure.NotNull(_chatContext);

            await this.PrepareContext(channel);

            using IDisposable typingState = channel.EnterTypingState();

            return _chatContext.ReadResponse(new ReadResponseSettings()
            {
                RespondingUser = user,
            }).First().Content;
        }

        public AutoRespond GetAutoRespond(ulong channelId)
        {
            if (_metaData.AutoResponds.TryGetValue(channelId, out AutoRespond? response))
            {
                return response;
            }
            else
            {
                return new AutoRespond();
            }
        }

        public ParsedMessage ParseMessage(IMessage message)
        {
            Ensure.NotNull(_character);

            ParsedMessage toReturn = new()
            {
                Content = message.Content
            };

            if (message.Content.Contains(ZERO_WIDTH))
            {
                toReturn.Content = message.Content.FromLast(ZERO_WIDTH)!.Trim();
            }

            if (message.Content.Length > 0 && message.Content[0] == ZERO_WIDTH)
            {
                toReturn.Author = message.Content.ToLast(ZERO_WIDTH)!.Trim(ZERO_WIDTH).Trim('*').Trim(':');
            }
            else if (message.Author.Id == _botId)
            {
                toReturn.Author = _chatSettings.BotName;
            }
            else if (_character.NameOverride.TryGetValue(message.Author.Username, out string? name))
            {
                toReturn.Author = name;
            }
            else if (message.Author is IGuildUser guildUser)
            {
                toReturn.Author = guildUser.DisplayName;
            }
            else
            {
                toReturn.Author = message.Author.Username;
            }

            return toReturn;
        }

        public async Task ProcessMessage(ISocketMessageChannel channel, ReadResponseSettings responseSettings)
        {
            if (!_processingSemaphore.Wait(0))
            {
                throw new AlreadyProcessingException();
            }

            try
            {
                Ensure.NotNull(_chatContext);

                await this.PrepareContext(channel);

                IMessage? prependMessage = null;
                string prependMessageContent = string.Empty;

                if (responseSettings.ContinueLast)
                {
                    prependMessage = await this.TryGetLastBotMessage(channel);

                    if (prependMessage != null)
                    {
                        prependMessageContent = this.ParseMessage(prependMessage).Content;
                    }
                }

                if (_chatSettings.ConditionalResponse &&
                    string.IsNullOrWhiteSpace(responseSettings.RespondingUser) &&
                    !responseSettings.ContinueLast)
                {
                    string nextUser = _chatContext.PredictNextUser().Trim();

                    if (nextUser != _chatSettings.BotName && !string.IsNullOrEmpty(nextUser))
                    {
                        return;
                    }
                }

                using IDisposable typingState = channel.EnterTypingState();

                foreach (ChatMessage cm in _chatContext.ReadResponse(responseSettings))
                {
                    string cmContent = cm.Content;

                    if (!string.IsNullOrWhiteSpace(prependMessageContent))
                    {
                        cmContent = prependMessageContent + cmContent;
                        prependMessageContent = string.Empty;
                    }

                    string content = this.BuildMessage(cm.User, cmContent);

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
            finally
            {
                _processingSemaphore.Release();
            }
        }

        public void SetAutoRespond(ulong channelId, string userName, bool disabled)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                _metaData.AutoResponds.Remove(channelId);
            }
            else
            {
                _metaData.AutoResponds[channelId] = new AutoRespond()
                {
                    Disabled = disabled,
                    UserName = userName,
                };
            }

            StaticConfiguration.Save(_metaData);
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

        public void TryProcessMessageAsync(ISocketMessageChannel smc, ReadResponseSettings? readResponseSettings = null)
        {
            readResponseSettings ??= new ReadResponseSettings();

            if (_processMessageThread is null || _processMessageThread.ThreadState != ThreadState.Running)
            {
                _processMessageThread = new Thread(async () =>
                {
                    try
                    {
                        await this.ProcessMessage(smc, readResponseSettings);
                    }
                    catch (AlreadyProcessingException) { }
                });

                _processMessageThread.Start();
            }
        }

        private IEnumerable<ChatMessage> HandleApplicationCommand(IMessage historicalMessage)
        {
            if (historicalMessage is RestUserMessage rmu)
            {
                if (string.IsNullOrWhiteSpace(rmu.Content))
                {
                    yield break;
                }

                if (rmu.InteractionMetadata is ApplicationCommandInteractionMetadata acm)
                {
                    if (acm.Name == "prompt")
                    {
                        yield break;
                    }

                    if (acm.Name == "clear")
                    {
                        yield break;
                    }
                }
            }

            yield break;
        }

        private IEnumerable<ChatMessage> HandleHistoricalMessage(IMessage historicalMessage)
        {
            ParsedMessage message = this.ParseMessage(historicalMessage);

            List<string> messageContent = [message.Content];

            if (_chatSettings.SplitSettings?.DoubleNewlineSplit ?? false)
            {
                messageContent = message.Content.Split("\n\n")
                                                .Select(s => s.Trim())
                                                .Where(s => !string.IsNullOrWhiteSpace(s))
                                                .Reverse()
                                                .ToList();
            }

            TokenMask contentMask = historicalMessage.Author.Id == _botId ?
                                                        TokenMask.Bot :
                                                        TokenMask.User;

            foreach (string s in messageContent)
            {
                yield return new ChatMessage(contentMask, message.Author, s);
            }
        }

        private void InsertContextHeaders(ulong channelId)
        {
            Ensure.NotNull(_chatContext);
            Ensure.NotNull(_character);

            string applicableSystemPrompt = DefaultSystemPrompt;

            if (SystemPrompts.TryGetValue(channelId, out string? channelPrompt))
            {
                applicableSystemPrompt = channelPrompt;
            }

            if (!string.IsNullOrWhiteSpace(applicableSystemPrompt))
            {
                if (_chatSettings.SystemPromptUser is null)
                {
                    _chatContext.SendContent(TokenMask.System, applicableSystemPrompt);
                }
                else
                {
                    _chatContext.SendMessage(TokenMask.System, _chatSettings.SystemPromptUser, applicableSystemPrompt);
                }
            }

            foreach (CharacterMessage cm in _character.ChatMessages)
            {
                if (string.IsNullOrWhiteSpace(cm.Content))
                {
                    throw new ArgumentNullException("A message with null content was found in the configuration");
                }

                _chatContext.SendMessage(cm, string.Equals(cm.User, _character.ChatSettings.BotName, StringComparison.OrdinalIgnoreCase));
            }
        }

        private async Task PrepareContext(ISocketMessageChannel channel)
        {
            Ensure.NotNull(_chatContext);

            Console.Clear();

            _chatContext.Clear(false);

            this.InsertContextHeaders(channel.GetChannelId());

            int messageStart = _chatContext.MessageCount;

            DateTime stop = DateTime.MinValue;

            if (_metaData.ClearValues.TryGetValue(channel.Id, out DateTime savedStop))
            {
                stop = savedStop;
            }

            DateTime? lastMessageTime = null;

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

                    List<ChatMessage> toSend = [];

                    if (historicalMessage.Type == MessageType.ApplicationCommand)
                    {
                        toSend.AddRange(this.HandleApplicationCommand(historicalMessage));
                    }
                    else
                    {
                        toSend.AddRange(this.HandleHistoricalMessage(historicalMessage));
                    }

                    DateTime thisMessageTime = historicalMessage.CreatedAt.DateTime.ToLocalTime();

                    if (lastMessageTime.HasValue)
                    {
                        TimeSpan timediff = lastMessageTime.Value - thisMessageTime;
                        if (timediff > TimeSpan.FromHours(1))
                        {
                            string displayString = timediff.ToDisplayString();
                            ChatMessage timeIndicator = new(TokenMask.System, _chatSettings.SystemPromptUser, $"No messages for {displayString} ({lastMessageTime.ToDisplayString()})");
                            _chatContext.Insert(messageStart, timeIndicator);
                        }
                    }

                    foreach (ChatMessage s in toSend)
                    {
                        _chatContext.Insert(messageStart, s);
                    }

                    lastMessageTime = thisMessageTime;

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

            ChatMessage startTimeIndicator = new(TokenMask.System, _chatSettings.SystemPromptUser, $"CURRENT TIME ({lastMessageTime.ToDisplayString()})");
            _chatContext.Insert(messageStart, startTimeIndicator);
        }

        public List<Token> Tokenize(string content)
        {
            Ensure.NotNull(_chatContext);

            return _chatContext.Tokenize(content);
        }
    }
}