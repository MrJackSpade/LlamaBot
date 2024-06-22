using Llama.Core;
using LlamaNative.Apis;
using LlamaNative.Chat.Interfaces;
using LlamaNative.Chat.Models;
using LlamaNative.Interfaces;
using LlamaNative.Interop.Settings;
using LlamaNative.Interop.Structs;
using LlamaNative.Models;
using LlamaNative.Samplers.Settings;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Sampling.Samplers.Temperature;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlamaNative.Chat
{
    public static class LlamaChatClient
    {

        public static IChatContext LoadChatContext(ChatSettings settings)
        {
            INativeContext context = LlamaClient.LoadContext(settings.ModelSettings,
                                                             settings.ContextSettings,
                                                             settings.TokenSelector,
                                                             settings.SimpleSamplers.ToArray());

            return new ChatContext(settings, context);
        }
    }
}
