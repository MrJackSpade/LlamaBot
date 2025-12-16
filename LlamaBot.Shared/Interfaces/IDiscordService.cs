using Discord;

namespace LlamaBot.Shared.Interfaces
{
    public interface IDiscordService
    {
        IUser CurrentUser { get; }

        string? BuildMessage(string author, string? content, bool prependDefaultUser);

        Task SendMessageAsync(IMessageChannel channel, string content, string? username = null, string? avatarUrl = null, bool prependDefaultUser = false);

        void SetAvatarUrl(ulong channelId, string username, string avatarUrl);
    }
}