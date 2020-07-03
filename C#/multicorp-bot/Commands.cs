using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace multicorp_bot
{
    public class Commands
    {

        Ranks ranks;
        Bank bank;

        public Commands()
        {
            ranks = new Ranks();
            bank = new Bank();
            Permissions.LoadPermissions();
        }

        [Command("check-requirements")]
        public async Task CheckRequirements(CommandContext ctx)
        {
            try
            {
                string missingRequirements = "";

                foreach (var item in ranks.MilRanks)
                {
                    if (!ctx.Guild.Roles.Select(x => x.Name).Contains(item.RankName))
                        missingRequirements += $"Rank {item.RankName} missing\n";
                }

                await ctx.RespondAsync(missingRequirements);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }

        [Command("check")]
        public async Task Check(CommandContext ctx, DiscordUser user)
        {
            try
            {
                var level = Permissions.GetPermissionLevel(ctx.Guild, user);
                Console.WriteLine(level);
                await ctx.RespondAsync($"The permission level of {user.Mention} is: {level}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        [Command("set-role-level")]
        public async Task SetRoleLevel(CommandContext ctx, DiscordRole role, int level)
        {
            if (Permissions.GetPermissionLevel(ctx.Guild, ctx.User) < 2)
                return;

            try
            {
                Permissions.SetRolePermissionLevel(role, level);
                await ctx.RespondAsync($"{role.Mention} is now assigned to level {level}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        [Command("promote")]
        public async Task PromoteMember(CommandContext ctx, DiscordMember member)
        {
            if (Permissions.GetPermissionLevel(ctx.Guild, ctx.User) < 1)
                return;
            Ranks r = new Ranks();
            await r.Promote(member);
            await ctx.RespondAsync($"Congratulations on your promotion {member.Mention} :partying_face:");
        }

        [Command("demote")]
        public async Task DemoteMember(CommandContext ctx, DiscordMember member)
        {
            if (Permissions.GetPermissionLevel(ctx.Guild, ctx.User) < 1)
                return;
            Ranks r = new Ranks();
            await r.Demote(member);
            await ctx.RespondAsync($"Oh no! What have you done {member.Mention}? :disappointed_relieved:");
        }

        [Command("recruit")]
        public async Task RecruitMember(CommandContext ctx, DiscordMember member)
        {
            if (Permissions.GetPermissionLevel(ctx.Guild, ctx.User) < 1)
                return;
            Ranks r = new Ranks();
            await r.Recruit(member);
            await ctx.RespondAsync($"Welcome on board {member.Mention} :alien:");
        }

        [Command("deposit")]
        public async Task Deposit(CommandContext ctx, DiscordMember member, int amount)
        {
            try
            {
                bank.Deposit(member, amount);

                //NAME NULL
                if (bank.GetBankStatusEmbedId() == 0)
                {
                    var msg = await ctx.RespondAsync("", false, bank.GetBankBalance());
                    bank.BankEmbedId = msg.Id;
                }
                else
                {
                    var msg = await ctx.Channel.GetMessageAsync(bank.GetBankStatusEmbedId());
                    await msg.ModifyAsync("", bank.GetBankBalance());
                }
            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message);
            }

        }

        [Command("withdraw")]
        public async Task Withdraw(CommandContext ctx, DiscordMember member, int amount)
        {
            try
            {
                bank.Withdraw(member, amount);

                if (bank.GetBankStatusEmbedId() == 0)
                {
                    var msg = await ctx.RespondAsync("", false, bank.GetBankBalance());
                    bank.BankEmbedId = msg.Id;
                }
                else
                {
                    var msg = await ctx.Channel.GetMessageAsync(bank.GetBankStatusEmbedId());
                    await msg.ModifyAsync("", bank.GetBankBalance());
                }
            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message);
            }

        }

        [Command("wipe-bank")]
        public async Task WipeBank(CommandContext ctx)
        {
            bank.WipeBank();
        }
    }
}