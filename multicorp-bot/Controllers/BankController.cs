using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using multicorp_bot.Controllers;
using multicorp_bot.Helpers;
using multicorp_bot.Models.DbModels;
using multicorp_bot.POCO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using System.Threading.Channels;
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

        public Bank AddBankEntry(DiscordGuild guild, bool isRp = false)
        {
            var orgId = new OrgController().GetOrgId(guild);

            List<Bank> getBank = MultiBotDb.Bank.AsQueryable().Where(x => x.OrgId == orgId).ToList();

            if (getBank.Count == 0 && !isRp)
            {
                var bank = new Bank()
                {
                    Balance = 0,
                    OrgId = orgId,
                };
                MultiBotDb.Bank.Add(bank);
                MultiBotDb.SaveChangesAsync();
            }
            else if (getBank.Count == 0 && isRp)
            {
                var icBank = new Bank()
                {
                    Balance = 0,
                    OrgId = orgId,
                    IsRp = true
                };

                var occBank = new Bank()
                {
                    Balance = 0,
                    OrgId = orgId,
                    IsRp = false
                };

                MultiBotDb.Bank.Add(icBank);
                MultiBotDb.Bank.Add(occBank);
                MultiBotDb.SaveChanges();
                
            }
            else
            {
                return getBank[0];
            }
            return GetBankByOrg(guild);
        }

        public Bank GetBankByOrg(DiscordGuild guild, bool isRp = false)
        {
            try
            {
                return new MultiBotDb().Bank.AsQueryable().Where(x => x.OrgId == new OrgController().GetOrgId(guild) && x.IsRp == isRp).First();
            }
            catch
            {
                return AddBankEntry(guild);
            }
        }

        public async Task<Tuple<string, string>> Deposit(BankTransaction trans)
        {
            var bankContext = MultiBotDb.Bank;
            var bankItem = GetBankByOrg(trans.Guild);
            bankItem.Balance = bankItem.Balance + trans.Amount;
            bankItem.Merits = bankItem.Merits + trans.Merits;

            bankContext.Update(bankItem);
            await MultiBotDb.SaveChangesAsync();

            return new Tuple<string, string> (FormatHelpers.FormattedNumber(GetBankBalance(trans.Guild).ToString()),
                FormatHelpers.FormattedNumber(GetBankMeritBalance(trans.Guild).ToString()));
        }

        public async Task<Tuple<string, string>> Withdraw(BankTransaction trans)
        {
            var bankContext = MultiBotDb.Bank;
            var bankItem = GetBankByOrg(trans.Guild);
            bankItem.Balance = bankItem.Balance - trans.Amount;
            bankItem.Merits = bankItem.Merits - trans.Merits;

            bankContext.Update(bankItem);
            await MultiBotDb.SaveChangesAsync();

            return new Tuple<string, string>(FormatHelpers.FormattedNumber(GetBankBalance(trans.Guild).ToString()),
                FormatHelpers.FormattedNumber(GetBankMeritBalance(trans.Guild).ToString()));
        }
         
        public DiscordMessageBuilder GetBankBalanceEmbed(DiscordGuild guild)
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

            return new DiscordMessageBuilder().AddEmbed(builder.Build());
        }

        public DiscordMessageBuilder GetRpBankBalanceEmbed(DiscordGuild guild)
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();

            builder.Title = $"{guild.Name} Bank";
            builder.Timestamp = DateTime.Now;

            var balance = GetBankBalance(guild, true);

            builder.AddField("Current Balance:", $"{FormatHelpers.FormattedNumber(balance.ToString())} aUEC", true);

            Expenses exp = GetExpenses(guild);
            builder.AddField(exp.Name, $"Monthly Amount: {exp.Amount} UEC\nMonthly Amount Remaining: {exp.Remaining} UEC");
            builder.AddField("Profit", balance - exp.Remaining >= 0 ? $"{balance - exp.Remaining} UEC": "0 UEC");

            DiscordComponent[] buttons = new DiscordComponent[2];
            buttons[0] = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "expense-update", "Update");
            buttons[1] = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "expense-period", "Start New Month");
            return new DiscordMessageBuilder().AddEmbed(builder.Build()).AddComponents(buttons);
        }

        public long? GetBankBalance(DiscordGuild guild, bool isRp = false)
        {
            try
            {
                var bankItem = GetBankByOrg(guild, isRp);
                return bankItem.Balance;
            } catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return 0;
        }

        public long? GetBankMeritBalance(DiscordGuild guild, bool isRp = false)
        {
            var bankItem = GetBankByOrg(guild, isRp);
            return bankItem.Merits;
        }

        public void WipeBank(DiscordGuild guild)
        {
            var bankContext = MultiBotDb.Bank;
            var bankItem = GetBankByOrg(guild);
            bankItem.Balance = 0;
            bankItem.Merits = 0;
            bankContext.Update(bankItem);
            MultiBotDb.SaveChanges();
        }

        public void UpdateTransaction(BankTransaction trans)
        {
            var memberC = new MemberController();
            var orgId = new OrgController().GetOrgId(trans.Guild);
            var memberId = memberC.GetMemberId(trans.Member.Username, orgId, trans.Member).GetValueOrDefault();
           
            var transC = new TransactionController();
            var transactionId = transC.GetTransactionId(memberId);

            transC.UpdateTransaction(transactionId, trans);
            MultiBotDb.SaveChanges();
        }

        public Tuple<string, string> Reconcile(CommandContext ctx,string merits, string credits)
        {
            var bank = MultiBotDb.Bank.Single(x => x.OrgId == new OrgController().GetOrgId(ctx.Guild));
            var differenceMerits = Math.Abs(bank.Merits - int.Parse(merits)).ToString();
            var differenceCredits = Math.Abs((int)bank.Balance - int.Parse(credits)).ToString();

            if (int.Parse(credits) < (int)bank.Balance)
            {
                differenceCredits = $"-{differenceCredits}";
            }
            if (int.Parse(merits) < (int)bank.Merits)
            {
                differenceMerits = $"-{differenceMerits}";
            }

            bank.Merits = int.Parse(merits);
            bank.Balance = int.Parse(credits);
            MultiBotDb.Bank.Update(bank);
            MultiBotDb.SaveChanges();



            return new Tuple<string, string>(FormatHelpers.FormattedNumber(differenceCredits.ToString()),
                FormatHelpers.FormattedNumber(differenceMerits.ToString()));
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

        public BankTransaction GetBankAction(CommandContext ctx, string action, int amount, DiscordMember member = null, string type = null)
        {
            BankTransaction trans;

            if (member == null)
            {
                trans = new BankTransaction(action, ctx.Member, ctx.Guild);
            }
            else
            {
                trans = new BankTransaction(action, member, ctx.Guild);
            }

            if (type == "merit")
            {
                trans.Merits = amount;
            }
            else
            {
                trans.Amount = amount;
            }

            return trans;
        }

        public Expenses GetExpenses(DiscordGuild guild)
        {
            int orgId = new OrgController().GetOrgId(guild);
            return MultiBotDb.Expenses.AsQueryable().Where(x => x.OrgId == new OrgController().GetOrgId(guild)).First();
        }

        public async Task UpdateExpense(DiscordGuild guild, int amount)
        {
            var exp = GetExpenses(guild);
            
            exp.Remaining = exp.Remaining - amount;
            MultiBotDb.Expenses.Update(exp);
            MultiBotDb.SaveChanges();

            var newDb = new MultiBotDb();
            var bankItem = GetBankByOrg(guild, true);
            bankItem.Balance = bankItem.Balance + amount;
            newDb.Bank.Update(bankItem);

            newDb.SaveChanges();

            await new Commands().updateRpBankBoard(guild, (await guild.GetChannelsAsync()).Where(x => x.Name == "bank" && x.Parent.Name == "In Character").First());
        }

        public async Task ExpenseButtonInteractionAsync(string interaction, DiscordGuild guild, DiscordChannel channel)
        {
            if (interaction == "update")
            {
                await new Commands().updateRpBankBoard(guild, channel);
            }
            else if (interaction == "period")
            {
                var exp = GetExpenses(guild);

                exp.Period = exp.Period++;
                exp.Remaining = exp.Amount;
                MultiBotDb.Expenses.Update(exp);

                MultiBotDb.SaveChanges();
                await new Commands().updateRpBankBoard(guild, channel);
            }
        }
    }
}