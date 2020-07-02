using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace multicorp_bot {
    public class Commands {
        [Command ("hi")]
        public async Task Hi (CommandContext ctx) {
            await ctx.RespondAsync ("Args:" + ctx.RawArgumentString);
        }

        [Command ("check")]
        public async Task Check (CommandContext ctx) {
            try {
                var level = PermissionResolver.GetPermissionLevel (ctx.Guild, ctx.Message.Author);
                System.Console.WriteLine (level);
                await ctx.RespondAsync ($"Your permission level is: {level}");
            } catch (Exception e) {
                System.Console.WriteLine(e.Message);
            }
        }

       [Command ("set-role-level")]
        public async Task SetRoleLevel (CommandContext ctx, DiscordRole role, int level) {
            try {
                PermissionResolver.SetRolePermissionLevel(ctx.Guild,role,level);
                await ctx.RespondAsync ($"{role.Mention} is now assigned to level {level}");
            } catch (Exception e) {
                System.Console.WriteLine(e.Message);
            }
        }
    }
}