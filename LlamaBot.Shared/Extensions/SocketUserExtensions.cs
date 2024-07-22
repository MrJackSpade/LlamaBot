using Discord.WebSocket;

namespace LlamaBot.Shared.Extensions
{
    public static class SocketUserExtensions
    {
        public static bool HasAnyRole(this SocketGuildUser sgu, params string[] roleNames)
        {
            foreach (string roleName in roleNames)
            {
                if (sgu.HasRole(roleName))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasRole(this SocketGuildUser sgu, string roleName, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        {
            return sgu.Roles.Any(r => string.Equals(r.Name, roleName, stringComparison));
        }
    }
}