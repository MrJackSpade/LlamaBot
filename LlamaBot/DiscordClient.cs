using Discord;
using Discord.WebSocket;

namespace LlamaBot
{
    internal class DiscordClient
    {
        public Func<SocketMessage, Task> MessageReceived;

        private readonly DiscordSocketClient _discordSocketClient;

        private readonly string _discordToken;

        public DiscordClient(string discordToken)
        {
            _discordToken = discordToken;
            _discordSocketClient = new DiscordSocketClient(new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.All
            });

            _discordSocketClient.MessageReceived += async (message) =>
            {
                if (MessageReceived != null)
                {
                    await MessageReceived(message);
                }
            };
        }

        public IUser CurrentUser => _discordSocketClient.CurrentUser;

        public async Task Connect()
        {
            TaskCompletionSource taskCompletionSource = new();
            _discordSocketClient.Ready += () =>
            {
                taskCompletionSource.SetResult();
                return Task.CompletedTask;
            };
            await _discordSocketClient.LoginAsync(TokenType.Bot, _discordToken);
            await _discordSocketClient.StartAsync();
            await taskCompletionSource.Task;
        }
    }
}