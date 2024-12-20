﻿using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Interfaces;
using Loxifi;

namespace LlamaBot.Plugins.EventArgs
{
    public struct InitializationEventArgs(string module, IPluginService pluginService, ILogger logger, IDiscordService discordService, ILlamaBotClient llamaBotClient)
    {
        private readonly string _module = module;

        public IDiscordService DiscordService { get; private set; } = discordService;

        public ILlamaBotClient LlamaBotClient { get; private set; } = llamaBotClient;

        public ILogger Logger { get; private set; } = logger;

        public IPluginService PluginService { get; set; } = pluginService;

        public readonly T LoadConfiguration<T>() where T : class, new()
        {
            string configurationDir = Directory.GetCurrentDirectory();

            configurationDir = Path.Combine(configurationDir, "Configurations");

            if (!Directory.Exists(configurationDir))
            {
                Directory.CreateDirectory(configurationDir);
            }

            configurationDir = Path.Combine(configurationDir, _module);

            if (!Directory.Exists(configurationDir))
            {
                Directory.CreateDirectory(configurationDir);
            }

            string configurationPath = Path.Combine(configurationDir, "Config.json");

            return StaticConfiguration.Load<T>(configurationPath);
        }
    }
}