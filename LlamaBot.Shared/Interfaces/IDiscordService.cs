using Discord;

namespace LlamaBot.Shared.Interfaces
{
    public interface IDiscordService
    {
        IUser CurrentUser { get; }
    }
}