using LlamaNative.Chat.Interfaces;
using LlamaNative.Extensions;
using LlamaNative.Interfaces;
using LlamaNative.Interop.Structs;
using LlamaNative.Models;
using LlamaNative.Tokens.Collections;
using LlamaNative.Tokens.Extensions;
using LlamaNative.Tokens.Models;
using System.Text;

namespace LlamaNative.Chat.Models
{
    internal class ChatContext(ChatSettings settings, INativeContext nativeContext) : IChatContext
    {
        private readonly object _lock = new();

        private readonly List<ChatMessage> _messages = [];

        private bool _running = false;

        public uint AvailableBuffer
        {
            get
            {
                TokenCollection contextTokens = NativeContext.Tokenize(this.ContextToString());

                return NativeContext.Size - contextTokens.Count;
            }
        }

        public int Count => _messages.Count;

        public int MessageCount => _messages.Count;

        public INativeContext NativeContext { get; private set; } = nativeContext;

        public ChatSettings Settings { get; private set; } = settings;

        public ChatMessage this[int index] => _messages[index];

        public uint CalculateLength(ChatMessage message)
        {
            TokenCollection tokenized = NativeContext.Tokenize(Settings.ChatTemplate.ToString(message));

            return tokenized.Count;
        }

        public void Clear()
        {
            lock (_lock)
            {
                _messages.Clear();
            }
        }

        public string ContextToString()
        {
            StringBuilder sb = new();

            if (!string.IsNullOrWhiteSpace(Settings.BeginText))
            {
                sb.Append(Settings.BeginText);
            }

            List<ChatMessage>? messages = null;

            lock (_lock)
            {
                messages = [.. _messages];
            }

            foreach (ChatMessage message in messages)
            {
                string messageContent = Settings.ChatTemplate.ToString(message);

                sb.Append(messageContent);
            }

            return sb.ToString();
        }

        public void Insert(int index, ChatMessage message)
        {
            lock (_lock)
            {
                _messages.Insert(index, message);
            }
        }

        public string PredictNextUser()
        {
            this.RefreshContext();

            NativeContext.Write(Settings.ChatTemplate.StartHeader);

            NativeContext.Evaluate();

            TokenCollection response = new();

            do
            {
                Token token = NativeContext.SelectToken();

                if (token.Value.Contains(Settings.ChatTemplate.EndHeader))
                {
                    break;
                }

                response.Append(token);
                NativeContext.Write(token);
                NativeContext.Evaluate();
            } while (true);

            return response.ToString();
        }

        public IEnumerable<ChatMessage> ReadResponse()
        {
            _running |= true;

            this.RefreshContext();

            NativeContext.Write(Settings.ChatTemplate.ToHeader(Settings.BotName));

            NativeContext.Evaluate();

            List<TokenSelection> response = new();

            do
            {
                if (!_running)
                {
                    break;
                }

                Token token = NativeContext.SelectToken(out SampleContext sampleContext);

                if (token.Value.Contains(Settings.ChatTemplate.EndMessage))
                {
                    break;
                }

                TokenSelection selection = new(token);

                if (Settings.SplitSettings != null && Settings.SplitSettings.MessageSplitId >= 0)
                {
                    try
                    {
                        TokenData data = sampleContext.OriginalCandidates.GetTokenData(Settings.SplitSettings.MessageSplitId);
                        selection.TokenData.Add(Settings.SplitSettings.MessageSplitId, data);
                    }
                    catch (KeyNotFoundException kex)
                    {
                        Console.WriteLine($"Token with id {Settings.SplitSettings.MessageSplitId} not found");
                    }
                }

                Console.Write(token);
                response.Add(selection);

                NativeContext.Write(token);
                NativeContext.Evaluate();
            } while (true);

            List<List<TokenSelection>> messageParts = this.RecursiveSplit(response).ToList();

            List<string> toReturn = [];

            for (int i = 0; i < messageParts.Count; i++)
            {
                List<TokenSelection> message = messageParts[i];

                string thisChunk = string.Join("", message.Select(s => s.SelectedToken.Value)).Trim();

                if (Settings?.SplitSettings?.DoubleNewlineSplit ?? false)
                {
                    foreach (string split in thisChunk.Split("\n\n"))
                    {
                        toReturn.Add(split);
                    }
                }
                else
                {
                    toReturn.Add(thisChunk);
                }
            }

            if (!_running)
            {
                //If interrupted, append interrupt.
                toReturn[^1] = toReturn[^1] + "-";
            }
            else
            {
                _running = false;
            }

            return toReturn.Select(s => new ChatMessage(Settings.BotName, s));
        }

        public void RemoveAt(int index) => _messages.RemoveAt(index);

        public void SendMessage(ChatMessage message)
        {
            lock (_lock)
            {
                _messages.Add(message);
            }
        }

        public bool TryInterrupt()
        {
            if (_running)
            {
                _running = false;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Splits the response into messages based on the ChatSettings requirements
        /// </summary>
        /// <param name="tokenSelections"></param>
        /// <returns></returns>
        private IEnumerable<List<TokenSelection>> RecursiveSplit(List<TokenSelection> tokenSelections)
        {
            int splitId = Settings.SplitSettings?.MessageSplitId ?? -1;
            int messageMin = Settings.SplitSettings?.MessageMinTokens ?? -1;
            int messageCurrent = string.Join("", tokenSelections.Select(s => s.SelectedToken.Value)).Length;

            if (Settings.SplitSettings is null || splitId < 0 || messageMin < 0)
            {
                yield return tokenSelections;
                yield break;
            }

            if (messageCurrent < Settings.SplitSettings.MessageMaxCharacters)
            {
                yield return tokenSelections;
                yield break;
            }

            //Trim min off both sides to ensure we're not cutting too short
            //TODO: Refactor this to use characters, like MAX
            int skip = messageMin;
            int take = tokenSelections.Count - (messageMin * 2);
            List<TokenSelection> checkTokens = tokenSelections.Skip(skip).Take(take).ToList();

            TokenSelection maxSplitDetected = checkTokens.OrderByDescending(t => t.TokenData[splitId].P).First();

            int splitIndex = tokenSelections.IndexOf(maxSplitDetected);

            List<TokenSelection> splitA = tokenSelections.Take(splitIndex).ToList();

            List<TokenSelection> splitB = tokenSelections.Skip(splitIndex).ToList();

            foreach (List<TokenSelection> cma in this.RecursiveSplit(splitA))
            {
                yield return cma;
            }

            foreach (List<TokenSelection> cmb in this.RecursiveSplit(splitB))
            {
                yield return cmb;
            }
        }

        private void RefreshContext()
        {
            NativeContext.Clear();

            NativeContext.Write(this.ContextToString());
        }
    }
}