using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using multicorp_bot.Controllers;
using multicorp_bot.Helpers;
using multicorp_bot.POCO;
using System;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
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

            builder.Title = $"{guild.Name} Bank";
            builder.Timestamp = DateTime.Now;

     
            builder.AddField("Current Balance:", $"{FormatHelpers.FormattedNumber(GetBankBalance(guild).ToString())} aUEC", true);
            builder.AddField("Current Merits:", $"{FormatHelpers.FormattedNumber(GetBankMeritBalance(guild).ToString())}", true);

            builder.AddField("\nTop Contributors", "Keep up the good work!", false).WithColor(DiscordColor.Red);

            foreach (var trans in new TransactionController().GetTopTransactions(guild))
            {
                builder.AddField(trans.MemberName, $"Credits: {FormatHelpers.FormattedNumber(trans.Amount.ToString())} aUEC");
            }

            builder.AddField("Top Merits Contributors", "Keep up the good work!", true).WithColor(DiscordColor.Red);

            foreach (var trans in new TransactionController().GetTopMeritTransactions(guild))
            {
                builder.AddField(trans.MemberName, $"{FormatHelpers.FormattedNumber(trans.Merits.ToString())} Merits");
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
            var memberId = 0;
            var orgId = new OrgController().GetOrgId(trans.Guild);
            if(trans.Member.Nickname != null)
            {
                memberId = memberC.GetMemberId(ranks.GetNickWithoutRank(trans.Member.Nickname), orgId, trans.Member).GetValueOrDefault();
            }
            else
            {
                memberId = memberC.GetMemberId(trans.Member.Username, orgId, trans.Member).GetValueOrDefault();
            }
            

            var transC = new TransactionController();
            var transactionId = transC.GetTransactionId(memberId);

            transC.UpdateTransaction(transactionId, trans);
            MultiBotDb.SaveChanges();
        }

        public decimal ExchangeTransaction(CommandContext ctx, string action, int credits, int merits)
        {
            var bankContext = MultiBotDb.Bank;
            var bankItem = GetBankByOrg(ctx.Guild);
            decimal margin = 0;

            if (action == "buy")
            {
                bankItem.Balance = bankItem.Balance - credits;
                bankItem.Merits = bankItem.Merits + merits;

                bankContext.Update(bankItem);
                MultiBotDb.SaveChanges();

                decimal cost = Convert.ToDecimal(credits);
                decimal grossmargin = Convert.ToDecimal(merits);

                margin = cost / (1 - grossmargin);

            }
            else if(action == "sell")
            {
                bankItem.Balance = bankItem.Balance + credits;
                bankItem.Merits = bankItem.Merits - merits;

                bankContext.Update(bankItem);
                MultiBotDb.SaveChanges();

                decimal cost = Convert.ToDecimal(credits);
                decimal grossmargin = Convert.ToDecimal(merits);
                
                margin = cost / (1 - grossmargin);
            }

            return Math.Round(Math.Abs(margin), 2);
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
            else if (args.Length == 4 && (ctx.Message.Content.ToLower().Contains("credit") || ctx.Message.Content.ToLower().Contains("merit")))
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
            else if (args.Length > 4 && (ctx.Message.Content.ToLower().Contains("credit") || ctx.Message.Content.ToLower().Contains("merit")))
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