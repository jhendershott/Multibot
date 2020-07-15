using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore.Internal;
using multicorp_bot.Controllers;
using multicorp_bot.Helpers;
using multicorp_bot.Models;
using multicorp_bot.POCO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace multicorp_bot
{
    public class BankController
    {
        MultiBotDb MultiBotDb;

        public BankController()
        {
            MultiBotDb = new MultiBotDb();
        }

        public Bank AddBankEntry(DiscordGuild guild)
        {
            var orgId = new OrgController().GetOrgId(guild);

            var bank = new Bank()
            {
                AccountId = GetHighestBankId() + 1,
                Balance = 0,
                OrgId = new OrgController().GetOrgId(guild),
            };

            MultiBotDb.Bank.Add(bank);
            MultiBotDb.SaveChangesAsync();

            return GetBankByOrg(guild);
        }

        public int GetHighestBankId()
        {
            return MultiBotDb.Bank.OrderByDescending(x => x.AccountId).First().AccountId;
        }

        public Bank GetBankByOrg(DiscordGuild guild)
        {
            try
            {
                return MultiBotDb.Bank.Single(x => x.OrgId == new OrgController().GetOrgId(guild));
            }
            catch
            {
                return AddBankEntry(guild);
            }
        }

        public string Deposit(BankTransaction trans)
        {
            var bankContext = MultiBotDb.Bank;
            OrgController orgC = new OrgController();
            var bankItem = GetBankByOrg(trans.Guild);
            bankItem.Balance = bankItem.Balance + trans.Amount;

            bankContext.Update(bankItem);
            MultiBotDb.SaveChanges();

            return FormatHelpers.FormattedNumber(GetBankBalance(trans.Guild).ToString());
        }

        public string Withdraw(BankTransaction trans)
        {
            var bankContext = MultiBotDb.Bank;
            OrgController orgC = new OrgController();
            var bankItem = GetBankByOrg(trans.Guild);
            bankItem.Balance = bankItem.Balance - trans.Amount;

            bankContext.Update(bankItem);
            MultiBotDb.SaveChanges();

            return FormatHelpers.FormattedNumber(GetBankBalance(trans.Guild).ToString());

        }
         
        public DiscordEmbed GetBankBalanceEmbed(DiscordGuild guild)
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            builder.Title = "MultiCorp Bank";
            builder.Timestamp = DateTime.Now;

            string amount = FormatHelpers.FormattedNumber(GetBankBalance(guild).ToString());
            builder.Description = $"Current Balance: {amount} aUEC";

            builder.AddField("Top Contributors", "Keep up the good work!", true).WithColor(DiscordColor.Red);


            foreach (var trans in new TransactionController().GetTopTransactions(guild))
            {
                builder.AddField(trans.MemberName, $"${FormatHelpers.FormattedNumber(trans.Amount.ToString())} aUEC");
            }

            return builder.Build();
        }

        public long? GetBankBalance(DiscordGuild guild)
        {

            var bankContext = MultiBotDb.Bank;
            OrgController orgC = new OrgController();
            var bankItem = GetBankByOrg(guild);
            return bankItem.Balance;
        }

        public void WipeBank(DiscordGuild guild)
        {
            var bankContext = MultiBotDb.Bank;
            OrgController orgC = new OrgController();
            var bankItem = GetBankByOrg(guild);
            bankItem.Balance = 0;
            bankContext.Update(bankItem);
            MultiBotDb.SaveChanges();
        }

        public void UpdateTransaction(BankTransaction trans)
        {
            Ranks ranks = new Ranks();
            var memberC = new MemberController();
            
            var orgId = new OrgController().GetOrgId(trans.Guild);
            var memberId = memberC.GetMemberId(ranks.GetNickWithoutRank(trans.Member.Nickname), orgId, trans.Member);

            var transC = new TransactionController();
            var transactionId = transC.GetTransactionId(memberId);

            transC.UpdateTransaction(transactionId, trans.Amount);
            MultiBotDb.SaveChanges();
        }

        public async Task<BankTransaction> GetBankActionAsync(CommandContext ctx)
        {
            string[] args = Regex.Split(ctx.Message.Content, @"\s+");


            if (args.Length == 3)
            {
                BankTransaction transaction = new BankTransaction(args[1], ctx.Member, int.Parse(args[2]), ctx.Guild);
                return transaction;
            }
            else if (args.Length == 4)
            {
                BankTransaction transaction = new BankTransaction(args[1], await ctx.Guild.GetMemberAsync(ctx.Message.MentionedUsers[0].Id), int.Parse(args[3]), ctx.Guild);
                return transaction;
            }
            else
            {
                await ctx.RespondAsync("Your transaction contains invalid arguments use !bank {action} {amount}");
                return null;
            }
        }
    }
}