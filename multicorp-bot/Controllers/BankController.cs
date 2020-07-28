using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using multicorp_bot.Controllers;
using multicorp_bot.Helpers;
using multicorp_bot.POCO;
using System;
using System.Linq;
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

        public Tuple<string, string> Deposit(BankTransaction trans)
        {
            var bankContext = MultiBotDb.Bank;
            var bankItem = GetBankByOrg(trans.Guild);
            bankItem.Balance = bankItem.Balance + trans.Amount;
            bankItem.Merits = bankItem.Merits + trans.Merits;

            bankContext.Update(bankItem);
            MultiBotDb.SaveChanges();

            return new Tuple<string, string> (FormatHelpers.FormattedNumber(GetBankBalance(trans.Guild).ToString()),
                FormatHelpers.FormattedNumber(GetBankMeritBalance(trans.Guild).ToString()));
        }

        public Tuple<string, string> Withdraw(BankTransaction trans)
        {
            var bankContext = MultiBotDb.Bank;
            var bankItem = GetBankByOrg(trans.Guild);
            bankItem.Balance = bankItem.Balance - trans.Amount;
            bankItem.Merits = bankItem.Merits - trans.Merits;

            bankContext.Update(bankItem);
            MultiBotDb.SaveChanges();

            return new Tuple<string, string>(FormatHelpers.FormattedNumber(GetBankBalance(trans.Guild).ToString()),
                FormatHelpers.FormattedNumber(GetBankMeritBalance(trans.Guild).ToString()));
        }
         
        public DiscordEmbed GetBankBalanceEmbed(DiscordGuild guild)
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            builder.Title = "MultiCorp Bank";
            builder.Timestamp = DateTime.Now;

     
            builder.AddField("Current Balance:", $"{FormatHelpers.FormattedNumber(GetBankBalance(guild).ToString())} aUEC", true);
            builder.AddField("Current Merits:", $"{FormatHelpers.FormattedNumber(GetBankMeritBalance(guild).ToString())}", true);

            builder.AddField("\nTop Contributors", "Keep up the good work!", false).WithColor(DiscordColor.Red);


            foreach (var trans in new TransactionController().GetTopTransactions(guild))
            {
                builder.AddField(trans.MemberName, $"Credits: {FormatHelpers.FormattedNumber(trans.Amount.ToString())} aUEC \nMerits: {FormatHelpers.FormattedNumber(trans.Merits.ToString())}");
            }

            return builder.Build();
        }

        public DiscordEmbed GetBankMeritEmbed(DiscordGuild guild)
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            builder.Title = "MultiCorp Bank";
            builder.Timestamp = DateTime.Now;

            string amount = FormatHelpers.FormattedNumber(GetBankBalance(guild).ToString());
            builder.Description = $"Current Balance: {amount} aUEC";

            builder.AddField("Top Contributors", "Keep up the good work!", true).WithColor(DiscordColor.Red);


            foreach (var trans in new TransactionController().GetTopMeritTransactions(guild))
            {
                builder.AddField(trans.MemberName, $"${FormatHelpers.FormattedNumber(trans.Amount.ToString())} aUEC");
            }

            return builder.Build();
        }

        public long? GetBankBalance(DiscordGuild guild)
        {
            var bankItem = GetBankByOrg(guild);
            return bankItem.Balance;
        }

        public long? GetBankMeritBalance(DiscordGuild guild)
        {
            var bankItem = GetBankByOrg(guild);
            return bankItem.Merits;
        }

        public void WipeBank(DiscordGuild guild)
        {
            var bankContext = MultiBotDb.Bank;
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

            transC.UpdateTransaction(transactionId, trans);
            MultiBotDb.SaveChanges();
        }

        public async Task<BankTransaction> GetBankActionAsync(CommandContext ctx, bool isCredits = true)
        {
            string[] args = Regex.Split(ctx.Message.Content, @"\s+");


            if (args.Length == 3)
            {
                if (isCredits)
                {
                    BankTransaction trans = new BankTransaction(args[1], ctx.Member, ctx.Guild, int.Parse(args[2]));
                    return trans;
                }
                else
                {
                    BankTransaction trans = new BankTransaction(args[1], ctx.Member, ctx.Guild, merits: int.Parse(args[2]));
                    return trans;
                }
            }
            else if (args.Length == 4)
            {
                if (isCredits)
                {
                    BankTransaction transaction = new BankTransaction(args[1], await ctx.Guild.GetMemberAsync(ctx.Message.MentionedUsers[0].Id), ctx.Guild, int.Parse(args[3]));
                    return transaction;
                }
                else
                {

                    BankTransaction transaction = new BankTransaction(args[1], await ctx.Guild.GetMemberAsync(ctx.Message.MentionedUsers[0].Id), ctx.Guild, merits: int.Parse(args[3]));
                    return transaction;
                }
            }
            else
            {
                await ctx.RespondAsync("Your transaction contains invalid arguments use !bank {action} {amount}");
                return null;
            }
        }
    }
}