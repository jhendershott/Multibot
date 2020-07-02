using System;
using System.Collections.Generic;
using System.Linq;
using DSharpPlus;
using DSharpPlus.Entities;

namespace multicorp_bot {
    public static class PermissionResolver {

        static Dictionary<DiscordGuild, Dictionary<int, DiscordRole>> GuildPermissions = new Dictionary<DiscordGuild, Dictionary<int, DiscordRole>> ();

        /*
         * Level 3 - Owner
         * Level 2 - Admin
         * Level 1 - Moderator
         */

        public static int GetPermissionLevel (DiscordGuild guild, DiscordUser user) {

            //Checks if user is owner or belongs to the level 3 role
            if (user == guild.Owner)
                return 3;
            else
                return  GetUserRoleLevel (guild, user);
        }

        public static void SetRolePermissionLevel(DiscordGuild guild,DiscordRole role, int level){
            if(!GuildPermissions.ContainsKey(guild))
                GuildPermissions.Add(guild, new Dictionary<int, DiscordRole>());

            if(!GuildPermissions[guild].ContainsKey(level)){
                GuildPermissions[guild].Add(level,role);
            }
            else
            {
                GuildPermissions[guild][level] = role;
            }
        }

        public static bool IsUsageAllowed (int requiredLevel, DiscordUser user, DiscordGuild guild) {
            return GetPermissionLevel (guild, user) >= requiredLevel;
        }

        private static int GetUserRoleLevel (DiscordGuild guild, DiscordUser user) {

            //Go through all roles and check if user is member of one of those and return the level
            for (int i = 0; i < GuildPermissions[guild].Count; i++) {
                if (guild.Members.Where (u => u == user).FirstOrDefault ().Roles.Contains (GuildPermissions[guild][i]))
                    return i;
            }

            //If not return -1
            return -1;
        }
    }
}