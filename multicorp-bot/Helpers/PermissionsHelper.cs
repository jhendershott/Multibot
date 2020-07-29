
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using multicorp_bot.Models;

namespace multicorp_bot
{
    public static class PermissionsHelper
    {

        static Dictionary<int, DiscordRole> GuildPermissions;
        const string PERM_DATA_PATH = "./perms.xml";

        /*
         * Level 3 - Owner
         * Level 2 - Admin
         * Level 1 - Moderator
         */

        public static void LoadPermissions()
        {
            if (File.Exists(PERM_DATA_PATH))
                GuildPermissions = (Dictionary<int, DiscordRole>)Serialization.Deserialize(typeof(Dictionary<int, DiscordRole>), PERM_DATA_PATH);
            else
                GuildPermissions = new Dictionary<int, DiscordRole>();
        }

        public static int GetPermissionLevel(DiscordGuild guild, DiscordUser user)
        {
            //Checks if user is owner or belongs to the level 3 role
            if (user == guild.Owner)
                return 3;
            else
                return GetUserRoleLevel(guild, user);
        }

        public static void SetRolePermissionLevel(DiscordRole role, int level)
        {
            if (!GuildPermissions.ContainsKey(level))
            {
                GuildPermissions.Add(level, role);
            }
            else
            {
                GuildPermissions[level] = role;
            }

            Serialization.Serialize(typeof(Dictionary<int, DiscordRole>),GuildPermissions, PERM_DATA_PATH);
        }

        public static bool IsUsageAllowed(int requiredLevel, DiscordUser user, DiscordGuild guild)
        {
            return GetPermissionLevel(guild, user) >= requiredLevel;
        }

        private static int GetUserRoleLevel(DiscordGuild guild, DiscordUser user)
        {
            //Go through all roles and check if user is member of one of those and return the level

            for (int i = 0; i < GuildPermissions.Count; i++)
            {
                if (guild.Members.Where(u => u == user).FirstOrDefault().Roles.Contains(GuildPermissions[i]))
                    return i;
            }

            //If not return -1
            return -1;
        }

        public static bool CheckPermissions(CommandContext ctx, Permissions perm)
        {
            var perms = ctx.Member.PermissionsIn(ctx.Channel);
            var test = perms.HasPermission(perm);
            return perms.HasPermission(perm);
        }
    }
}