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
        private readonly List<ChatMessage> _messages = [];

        private readonly object _lock = new();

        public ChatContext(ChatSettings settings, INativeContext nativeContext)
        {
            Settings = settings;
            NativeContext = nativeContext;
        }

        public int Count => _messages.Count;

        public INativeContext NativeContext { get; private set; }

        public ChatSettings Settings { get; private set; }

        public ChatMessage this[int index] => _messages[index];

        private void RefreshContext()
        {
            NativeContext.SetBufferPointer(0);

            if (!string.IsNullOrWhiteSpace(Settings.BeginText))
            {
                NativeContext.Write(Settings.BeginText);
            }

            List<ChatMessage>? messages = null;

            lock (_lock)
            {
                messages = [.. _messages];
            }

            foreach (ChatMessage message in messages)
            {
                string messageContent = ToString(message);

                NativeContext.Write(messageContent);

                NativeContext.Write("\n");
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

        private string ToHeader(ChatMessage message)
        {
            ChatTemplate template = Settings.ChatTemplate;
            StringBuilder sb = new();
            sb.Append(template.StartHeader);
            sb.Append(message.User);
            sb.Append(template.EndHeader);
            return sb.ToString();
        }

        private string ToString(ChatMessage message)
        {
            ChatTemplate template = Settings.ChatTemplate;
            StringBuilder sb = new();
            sb.Append(ToHeader(message));
            sb.Append(message.Content);
            sb.Append(template.EndMessage);
            return sb.ToString();
        }
    }
}