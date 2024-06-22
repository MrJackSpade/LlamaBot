using LlamaNative.Chat.Interfaces;
using LlamaNative.Interfaces;
using LlamaNative.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LlamaNative.Tokens.Collections;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Chat.Models
{
    internal class ChatContext : IChatContext
    {
        private readonly List<ChatMessage> _messages = [];

        public ChatContext(ChatSettings settings, INativeContext nativeContext)
        {
            Settings = settings;
            NativeContext = nativeContext;
        }

        public INativeContext NativeContext { get; private set; }

        public ChatSettings Settings { get; private set; }

        public ChatMessage ReadResponse()
        {
            NativeContext.SetBufferPointer(0);

            if (!string.IsNullOrWhiteSpace(Settings.BeginText))
            {
                NativeContext.Write(Settings.BeginText);
            }

            foreach (ChatMessage message in _messages)
            {
                string messageContent = ToString(message);

                NativeContext.Write(messageContent);

                NativeContext.Write("\n");
            }

            ChatMessage responseMessage = new()
            {
                User = Settings.BotName
            };

            NativeContext.Write(ToHeader(responseMessage));

            NativeContext.Evaluate();

            TokenCollection response = new();

            do
            {
                Token token = NativeContext.SampleNext();

                Console.Write(token);
                response.Append(token);

                NativeContext.Write(token);
                NativeContext.Evaluate();

                string responseContent = response.ToString();

                if (responseContent.Contains(Settings.ChatTemplate.EndMessage))
                {
                    break;
                }
            } while (true);

            responseMessage.Content = response.ToString();

            return responseMessage;
        }

        public void SendMessage(ChatMessage message)
        {
            _messages.Add(message);
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