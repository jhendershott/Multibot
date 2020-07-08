using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using multicorp_bot.POCO;

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

        [Command("handle")]
        public async Task UpdateHandle(CommandContext ctx)
        {
            string[] args = Regex.Split(ctx.Message.Content, @"\s+");
            DiscordMember member = null;
            string newNick = null;
            if(args.Length == 2)
            {
                member = ctx.Member;
                newNick = ranks.GetUpdatedNickname(member, args[1]);
            }
            else if(args.Length >= 3)
            {
                member = await ctx.Guild.GetMemberAsync(ctx.Message.MentionedUsers[0].Id);
                newNick = ranks.GetUpdatedNickname(member, args[2]);
            }

            ranks.Psql.UpdateNickName(ranks.GetNickWithoutRank(member), ranks.GetNickWithoutRank(newNick), ctx.Guild);
            await member.ModifyAsync(nickname: newNick);
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
            await ranks.Promote(member);
            await member.ModifyAsync(ranks.GetUpdatedNickname(member));
            await ctx.RespondAsync($"Congratulations on your promotion {member.Mention} :partying_face:");
        }

        [Command("demote")]
        public async Task DemoteMember(CommandContext ctx, DiscordMember member)
        {
            if (Permissions.GetPermissionLevel(ctx.Guild, ctx.User) < 1)
                return;
            await ranks.Demote(member);
            await member.ModifyAsync(ranks.GetUpdatedNickname(member, -1));
            await ctx.RespondAsync($"Oh no you've been demoted! What have you done {member.Mention}? :disappointed_relieved:");
        }

        [Command("recruit")]
        public async Task RecruitMember(CommandContext ctx, DiscordMember member)
        {
            if (Permissions.GetPermissionLevel(ctx.Guild, ctx.User) < 1)
                return;
            await ranks.Recruit(member);
            await ctx.RespondAsync($"Welcome on board {member.Mention} :alien:");
        }

        [Command("bank")]
        public async Task Bank(CommandContext ctx)
        {
            string[] args = Regex.Split(ctx.Message.Content, @"\s+");
            string newBalance;
            BankTransaction transaction = null;
            try
            {
                switch (args[1].ToLower())
                {
                    case "deposit":
                        transaction = await bank.GetBankActionAsync(ctx);
                        newBalance = bank.Deposit(transaction);
                        bank.UpdateTransaction(transaction);
                        await ctx.RespondAsync($"Thank you for your contribution of {transaction.Amount}! The new bank balance is {newBalance}");
                        break;
                    case "withdraw":
                        transaction = await bank.GetBankActionAsync(ctx);
                        newBalance = bank.Withdraw(transaction);
                        await ctx.RespondAsync($"You have successfully withdrawn {transaction.Amount}. The new bank balance is {newBalance}");
                        break;
                    case "balance":
                        var balanceembed = bank.GetBankBalance(ctx.Guild);
                        await ctx.RespondAsync(embed: balanceembed);
                        break;
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
            bank.WipeBank(ctx.Guild);
            await ctx.RespondAsync("Your org balance and transactions have been set to 0");

        }
    }
}