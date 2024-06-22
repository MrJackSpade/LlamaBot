using LlamaNative;
using LlamaNative.Chat;
using LlamaNative.Chat.Interfaces;
using LlamaNative.Chat.Models;
using LlamaNative.Extensions;
using LlamaNative.Interfaces;
using LlamaNative.Interop.Settings;
using LlamaNative.Tokens.Models;

namespace LlamaBot
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string modelPath = @"C:\Users\Service Account\Downloads\L3-8B-Stheno-v3.2-Q8_0-imat.gguf";

            IChatContext context = LlamaChatClient.LoadChatContext(new ChatSettings
            {
                BotName = "LlamaBot",
                BeginText = "<|begin_of_text|>",
                ModelSettings = new ModelSettings
                {
                    ModelPath = modelPath
                },
                ChatTemplate = new ChatTemplate
                {
                    EndHeader = "<|end_header_id|>\n",
                    EndMessage = "<|eot_id|>",
                    StartHeader = "<|start_header_id|>",
                }
            });

            context.SendMessage("User", "Hello, LlamaBot!");

            ChatMessage response = context.ReadResponse();

            context.SendMessage(response);

            context.SendMessage("User", "Goodbye, LlamaBot!");

            response = context.ReadResponse();
        }
    }
}