using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using LlamaBot.Discord.Attributes;
using LlamaBot.Discord.Exceptions;
using LlamaBot.Discord.Extensions;
using LlamaBot.Plugins.EventArgs;
using LlamaBot.Shared.Models;
using LlamaNative.Utils;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using DiscordNet = Discord.Net;
using IDiscordService = LlamaBot.Shared.Interfaces.IDiscordService;

namespace LlamaBot.Discord
{
    public class DiscordService : IDiscordService
    {
        public Func<SocketMessage, Task>? MessageReceived;

        private const string AVATAR_CACHE_FILENAME = "avatar_cache.json";

        private const string WEBHOOK_NAME = "LlamaBot Proxy";

        private const char ZERO_WIDTH = (char)8203;

        private readonly object _avatarCacheLock = new();

        private readonly string _avatarCachePath;

        private readonly Dictionary<string, Func<SocketSlashCommand, Task<CommandResult>>> _commandCallbacks = [];

        private readonly DiscordSocketClient _discordClient;

        private readonly string _discordToken;

        private readonly Dictionary<ulong, IWebhook> _webhookCache = [];

        private readonly object _webhookCacheLock = new();

        private bool _avatarCacheLoaded = false;

        private Dictionary<ulong, Dictionary<string, string>> _avatarUrlCache = [];

        private bool _connected;

        public void SetAvatarUrl(ulong channelId, string username, string avatarUrl)
        {
            lock (_avatarCacheLock)
            {
                this.LoadAvatarCacheIfNeeded();
                if (!_avatarUrlCache.TryGetValue(channelId, out Dictionary<string, string>? channelCache))
                {
                    channelCache = [];
                    _avatarUrlCache[channelId] = channelCache;
                }

                channelCache[username] = avatarUrl;
                this.SaveAvatarCache();
            }
        }

        public DiscordService(string? discordToken)
        {
            Ensure.NotNullOrWhiteSpace(discordToken);

            _discordToken = discordToken;

            _avatarCachePath = Path.Combine(AppContext.BaseDirectory, AVATAR_CACHE_FILENAME);

            _discordClient = new DiscordSocketClient(new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.All
            });

            _discordClient.SlashCommandExecuted += this.SlashCommandHandler;

            _discordClient.MessageReceived += async (message) =>
            {
                if (MessageReceived != null)
                {
                    await MessageReceived(message);
                }
            };
        }

        public IUser CurrentUser => _discordClient.CurrentUser;

        public Func<ReactionEventArgs, Task>? ReactionAdded { get; set; }

        public async Task AddCommand(string command, string description, Type t, Func<BaseCommand, Task<CommandResult>> action, params SlashCommandOption[] slashCommandOptions)
        {
            command = command.ToLower();

            _commandCallbacks.Add(command, (c) => action.Invoke(CastType(c, t)));

            await this.AddCommand(command, description, t, slashCommandOptions);
        }

        public string BuildMessage(string author, string content, bool prependDefaultUser)
        {
            string header = string.Empty;

            author ??= CurrentUser.Username;

            if (author != CurrentUser.Username || prependDefaultUser)
            {
                header += $"{ZERO_WIDTH}**{author}:**{ZERO_WIDTH}";
            }

            return header + content;
        }

        public async Task Connect()
        {
            if (!_connected)
            {
                TaskCompletionSource taskCompletionSource = new();
                _discordClient.Ready += () =>
                {
                    if (!taskCompletionSource.Task.IsCompleted)
                    {
                        taskCompletionSource.SetResult();
                    }

                    _connected = true;

                    return Task.CompletedTask;
                };
                await _discordClient.LoginAsync(TokenType.Bot, _discordToken);
                await _discordClient.StartAsync();
                await taskCompletionSource.Task;
            }
        }

        public async Task SendMessageAsync(IMessageChannel channel, string content, string? username = null, string? avatarUrl = null, bool prependDefaultUser = false)
        {
            username ??= CurrentUser.Username;

            if (channel is not ITextChannel)
            {
                // Webhooks don't work in DMs, fall back to normal send
                await channel.SendMessageAsync(this.BuildMessage(username, content, prependDefaultUser));
                return;
            }

            string? resolvedAvatarUrl = this.GetOrUpdateAvatarUrl(channel.Id, username, avatarUrl);

            (IWebhook? webhook, ulong? threadId) = await this.GetOrCreateWebhookAsync(channel);

            if (webhook == null)
            {
                // Couldn't get/create webhook, fall back to normal send
                await channel.SendMessageAsync(this.BuildMessage(username, content, prependDefaultUser));
                return;
            }

            try
            {
                await this.SendViaWebhookAsync(webhook, content, username!, resolvedAvatarUrl, threadId);
            }
            catch (DiscordNet.HttpException ex) when (ex.DiscordCode == DiscordErrorCode.UnknownWebhook)
            {
                // Webhook was deleted externally, clear cache and retry once
                this.ClearWebhookCache(channel);

                (webhook, threadId) = await this.GetOrCreateWebhookAsync(channel);

                if (webhook != null)
                {
                    await this.SendViaWebhookAsync(webhook, content, username!, resolvedAvatarUrl, threadId);
                }
                else
                {
                    await channel.SendMessageAsync(this.BuildMessage(username, content, prependDefaultUser));
                }
            }
        }

        internal async Task SetUserName(string botName)
        {
            await _discordClient.CurrentUser.ModifyAsync(s => s.Username = botName);
        }

        private static BaseCommand CastType(SocketSlashCommand source, Type t)
        {
            if (!typeof(BaseCommand).IsAssignableFrom(t))
            {
                throw new ArgumentException("Cast type must inherit from base command");
            }

            BaseCommand payload = (BaseCommand)Activator.CreateInstance(t, [source])!;

            Dictionary<string, PropertyInfo> propertyDict = [];

            foreach (PropertyInfo pi in t.GetProperties())
            {
                string name = pi.Name.ToLower();

                if (pi.GetCustomAttribute<DisplayAttribute>() is DisplayAttribute d)
                {
                    if (!string.IsNullOrWhiteSpace(d.Name))
                    {
                        name = d.Name.ToLower();
                    }
                }

                propertyDict.Add(name, pi);
            }

            foreach (SocketSlashCommandDataOption? option in source.Data.Options)
            {
                try
                {
                    if (propertyDict.TryGetValue(option.Name, out PropertyInfo prop))
                    {
                        if (prop.PropertyType == typeof(List<string>))
                        {
                            bool isDistinct = prop.GetCustomAttribute<DistinctAttribute>() is not null;

                            string v = option.Value.ToString();

                            List<string> values = [];

                            if (!string.IsNullOrWhiteSpace(v))
                            {
                                foreach (string part in v.Split(',', ';').Select(l => l.Trim()))
                                {
                                    values.Add(part);
                                }

                                if (isDistinct)
                                {
                                    values = values.Distinct().ToList();
                                }

                                prop.SetValue(payload, values);
                            }
                        }
                        else if (prop.PropertyType == typeof(bool))
                        {
                            string b = option.Value.ToString().ToLower();

                            prop.SetValue(payload, b != "false");
                        }
                        else if (prop.PropertyType.IsEnum)
                        {
                            if (!Enum.TryParse(prop.PropertyType, option.Value.ToString(), out object value))
                            {
                                throw new CommandPropertyValidationException(option.Name, $"'{option.Value}' is not a valid value");
                            }
                            else
                            {
                                prop.SetValue(payload, value);
                            }
                        }
                        else if (option.Value.GetType() == prop.PropertyType)
                        {
                            try
                            {
                                prop.SetValue(payload, option.Value);
                            }
                            catch (Exception ex)
                            {
                                throw new CommandPropertyValidationException(option.Name, $"Could not assign value '{option.Value}' type '{option.Value?.GetType()}' to type '{prop.PropertyType}'");
                            }
                        }
                        else
                        {
                            try
                            {
                                prop.SetValue(payload, Convert.ChangeType(option.Value, prop.PropertyType));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                throw new CommandPropertyValidationException(option.Name, $"Could not cast value '{option.Value}' to type '{prop.PropertyType}'");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            return payload;
        }

        private async Task AddCommand(string command, string description, Type t, params SlashCommandOption[] slashCommandOptions)
        {
            SlashCommandBuilder commandBuilder = new SlashCommandBuilder()
                .WithName(command)
                .WithDescription(description)
                .WithContextTypes(InteractionContextType.BotDm | InteractionContextType.Guild);

            slashCommandOptions ??= [];

            foreach (SlashCommandOption option in slashCommandOptions)
            {
                commandBuilder.AddOption(option);
            }

            foreach (PropertyInfo property in t.GetProperties())
            {
                commandBuilder.TryAddOption(property);
            }

            await _discordClient.CreateGlobalApplicationCommandAsync(commandBuilder.Build());

            foreach (IGuild guild in _discordClient.Guilds)
            {
                await guild.CreateApplicationCommandAsync(commandBuilder.Build());
            }
        }

        private async Task<(IWebhook? Webhook, ulong? ThreadId)> GetOrCreateWebhookAsync(IMessageChannel channel)
        {
            ulong? threadId = null;
            IIntegrationChannel targetChannel;

            // Check if this is a thread - webhooks must be created on parent channel
            if (channel is IThreadChannel threadChannel)
            {
                threadId = threadChannel.Id;

                // Get parent channel - works for both text channel threads and forum posts
                if (threadChannel is SocketThreadChannel socketThread &&
                    socketThread.ParentChannel is IIntegrationChannel parentChannel)
                {
                    targetChannel = parentChannel;
                }
                else
                {
                    Debug.WriteLine($"Could not resolve parent channel for thread {threadChannel.Id}");
                    return (null, null);
                }
            }
            else if (channel is IIntegrationChannel integrationChannel)
            {
                targetChannel = integrationChannel;
            }
            else
            {
                // DMs and other unsupported channel types
                return (null, null);
            }

            ulong parentChannelId = ((IChannel)targetChannel).Id;

            lock (_webhookCacheLock)
            {
                if (_webhookCache.TryGetValue(parentChannelId, out IWebhook? cachedWebhook))
                {
                    return (cachedWebhook, threadId);
                }
            }

            try
            {
                IReadOnlyCollection<IWebhook> webhooks = await targetChannel.GetWebhooksAsync();

                IWebhook? existingWebhook = webhooks.FirstOrDefault(w =>
                    w.Creator?.Id == CurrentUser.Id &&
                    w.Name == WEBHOOK_NAME);

                if (existingWebhook != null)
                {
                    lock (_webhookCacheLock)
                    {
                        _webhookCache[parentChannelId] = existingWebhook;
                    }

                    return (existingWebhook, threadId);
                }

                IWebhook newWebhook = await targetChannel.CreateWebhookAsync(WEBHOOK_NAME);

                lock (_webhookCacheLock)
                {
                    _webhookCache[parentChannelId] = newWebhook;
                }

                return (newWebhook, threadId);
            }
            catch (DiscordNet.HttpException ex)
            {
                Debug.WriteLine($"Failed to get/create webhook for channel {parentChannelId}: {ex.Message}");
                return (null, null);
            }
        }

        private void ClearWebhookCache(IMessageChannel channel)
        {
            ulong cacheKey;

            if (channel is SocketThreadChannel socketThread &&
                socketThread.ParentChannel is IChannel parentChannel)
            {
                cacheKey = parentChannel.Id;
            }
            else
            {
                cacheKey = channel.Id;
            }

            lock (_webhookCacheLock)
            {
                _webhookCache.Remove(cacheKey);
            }
        }

        private string? GetOrUpdateAvatarUrl(ulong channelId, string username, string? providedUrl)
        {
            lock (_avatarCacheLock)
            {
                this.LoadAvatarCacheIfNeeded();

                if (!_avatarUrlCache.TryGetValue(channelId, out Dictionary<string, string>? channelCache))
                {
                    channelCache = [];
                    _avatarUrlCache[channelId] = channelCache;
                }

                string? cachedUrl = channelCache.TryGetValue(username, out string? existing) ? existing : null;

                if (!string.IsNullOrWhiteSpace(providedUrl))
                {
                    if (!string.Equals(cachedUrl, providedUrl, StringComparison.Ordinal))
                    {
                        channelCache[username] = providedUrl;
                        this.SaveAvatarCache();
                    }

                    return providedUrl;
                }

                return cachedUrl;
            }
        }

        private void LoadAvatarCacheIfNeeded()
        {
            if (_avatarCacheLoaded)
            {
                return;
            }

            if (File.Exists(_avatarCachePath))
            {
                try
                {
                    string json = File.ReadAllText(_avatarCachePath);

                    Dictionary<ulong, Dictionary<string, string>>? loaded =
                        JsonSerializer.Deserialize<Dictionary<ulong, Dictionary<string, string>>>(json);

                    if (loaded != null)
                    {
                        _avatarUrlCache = loaded;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to load avatar cache: {ex.Message}");
                    _avatarUrlCache = [];
                }
            }

            _avatarCacheLoaded = true;
        }

        private void SaveAvatarCache()
        {
            try
            {
                JsonSerializerOptions options = new()
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(_avatarUrlCache, options);

                File.WriteAllText(_avatarCachePath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save avatar cache: {ex.Message}");
            }
        }

        private async Task SendViaWebhookAsync(IWebhook webhook, string content, string username, string? avatarUrl, ulong? threadId = null)
        {
            using DiscordWebhookClient client = new(webhook);

            await client.SendMessageAsync(
                text: content,
                username: username,
                avatarUrl: avatarUrl,
                threadId: threadId
            );
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (_commandCallbacks.TryGetValue(command.CommandName, out Func<SocketSlashCommand, Task<CommandResult>>? callback))
            {
                CommandResult result = CommandResult.Error("You should never see this message");

                try
                {
                    await command.DeferAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"An exception occurred while deferring the message {ex}");
                }

                try
                {
                    result = await callback.Invoke(command);
                }
                catch (CommandPropertyValidationException cex)
                {
                    result = CommandResult.Error($"{cex.PropertyName}: {cex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    result = CommandResult.Error(ex.Message);
                }

                if (!result.IsSuccess)
                {
                    try
                    {
                        await command.FollowupAsync(result.Message, ephemeral: true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(result.Message))
                {
                    await command.FollowupAsync(result.Message);
                }
                else if (result.FileData is not null)
                {
                    await command.FollowupWithFileAsync(new MemoryStream(result.FileData), result.FileName ?? "response.txt");
                }
            }
        }
    }
}