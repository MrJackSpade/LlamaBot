using Discord;
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
using IDiscordService = LlamaBot.Shared.Interfaces.IDiscordService;

namespace LlamaBot.Discord
{
    public class DiscordService : IDiscordService
    {
        public Func<SocketMessage, Task>? MessageReceived;

        private readonly Dictionary<string, Func<SocketSlashCommand, Task<CommandResult>>> _commandCallbacks = [];

        private readonly DiscordSocketClient _discordClient;

        private readonly string _discordToken;

        private bool _connected;

        public DiscordService(string? discordToken)
        {
            Ensure.NotNullOrWhiteSpace(discordToken);

            _discordToken = discordToken;

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

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (_commandCallbacks.TryGetValue(command.CommandName, out Func<SocketSlashCommand, Task<CommandResult>> callback))
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
            }
        }
    }
}