using LlamaNative.Chat.Interfaces;
using LlamaNative.Extensions;
using LlamaNative.Interfaces;
using LlamaNative.Tokens.Collections;
using LlamaNative.Tokens.Models;
using System.Text;

namespace LlamaNative.Chat.Models
{
    internal class ChatContext : IChatContext
    {
        private readonly object _lock = new();

        private readonly List<ChatMessage> _messages = [];

        public ChatContext(ChatSettings settings, INativeContext nativeContext)
        {
            Settings = settings;
            NativeContext = nativeContext;
        }

        public uint AvailableBuffer 
        {
            get
            {
                TokenCollection contextTokens = NativeContext.Tokenize(ContextToString());

                return Settings.ContextSettings.ContextSize - contextTokens.Count;
            }
        }

        public int Count => _messages.Count;

        public INativeContext NativeContext { get; private set; }

        public ChatSettings Settings { get; private set; }

        public ChatMessage this[int index] => _messages[index];

        public string PredictNextUser()
        {
            RefreshContext();

            NativeContext.Write(Settings.ChatTemplate.StartHeader);

            NativeContext.Evaluate();

            TokenCollection response = new();

            do
            {
                Token token = NativeContext.SampleNext();

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

        public uint CalculateLength(ChatMessage message)
        {
            TokenCollection tokenized = NativeContext.Tokenize(ToString(message));

            return tokenized.Count;
        }

        public void Clear()
        {
            lock (_lock)
            {
                _messages.Clear();
            }
        }

        public void Insert(int index, ChatMessage message)
        {
            lock (_lock)
            {
                _messages.Insert(index, message);
            }
        }

        public ChatMessage ReadResponse()
        {
            RefreshContext();

            ChatMessage responseMessage = new(Settings.BotName);

            NativeContext.Write(ToHeader(responseMessage));

            NativeContext.Evaluate();

            TokenCollection response = new();

            do
            {
                Token token = NativeContext.SampleNext();

                if (token.Value.Contains(Settings.ChatTemplate.EndMessage))
                {
                    break;
                }

                Console.Write(token);
                response.Append(token);

                NativeContext.Write(token);
                NativeContext.Evaluate();
            } while (true);

            responseMessage.Content = response.ToString();

            return responseMessage;
        }

        public void RemoveAt(int index) => _messages.RemoveAt(index);

        public void SendMessage(ChatMessage message)
        {
            lock (_lock)
            {
                _messages.Add(message);
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
                string messageContent = ToString(message);

                sb.Append(messageContent);
            }

            return sb.ToString();
        }

        private void RefreshContext()
        {
            NativeContext.Clear();

            NativeContext.Write(ContextToString());
        }

        private string ToHeader(ChatMessage message)
        {
            ChatTemplate template = Settings.ChatTemplate;
            StringBuilder sb = new();
            sb.Append(template.StartHeader);
            sb.Append(message.User);
            sb.Append(template.EndHeader);

            if(template.HeaderNewline)
            {
                sb.Append('\n');
            }

            return sb.ToString();
        }

        private string ToString(ChatMessage message)
        {
            ChatTemplate template = Settings.ChatTemplate;
            StringBuilder sb = new();
            sb.Append(ToHeader(message));
            sb.Append(message.Content);
            sb.Append(template.EndMessage);

            if (template.MessageNewline)
            {
                sb.Append('\n');
            }

            return sb.ToString();
        }
    }
}