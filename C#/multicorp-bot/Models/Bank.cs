using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using multicorp_bot.Models;
using multicorp_bot.POCO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace multicorp_bot
{
    public class Bank
    {
        Psql Psql;
        BankData bankData;
        const string BANK_DATA_PATH = "./bank.xml";

        public ulong BankEmbedId { set { bankData.BankEmbed = value; } }

        public Bank()
        {
            Psql = new Psql();
            try
            {
                if (File.Exists(BANK_DATA_PATH))
                    bankData = (BankData)Serialization.Deserialize(typeof(BankData), BANK_DATA_PATH);
                else
                {
                    bankData = new BankData();
                    bankData.BankBalance = 0;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void Deposit(DiscordMember user, int amount)
        {
            try
            {
                bankData.Transactions.Add(new KeyValuePair<DiscordMember, int>(user, amount));
                bankData.BankBalance += amount;
                SaveBankData();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }

        public string Deposit(BankTransaction trans)
        {
   
            Psql.UpdateBankBalance(trans.Amount, trans.Guild);
            return FormattedNumber(Psql.GetBankBalance(trans.Guild));
        }

        public string Withdraw(BankTransaction trans)
        {
            
            Psql.UpdateBankBalance(-trans.Amount, trans.Guild);
            return FormattedNumber(Psql.GetBankBalance(trans.Guild));

        }

        public DiscordEmbed GetBankBalance(DiscordGuild guild)
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            builder.Title = "MultiCorp Bank";
            builder.Timestamp = DateTime.Now;
            
            string amount  = FormattedNumber(Psql.GetBankBalance(guild));
            builder.Description = $"Current Balance: {amount} aUEC";

            builder.AddField("Top Contributors", "Keep up the good work!", true).WithColor(DiscordColor.Red);
            foreach (var trans in Psql.GetOrgTopTransactions(guild))
            {
                builder.AddField(trans.Item1, FormattedNumber(trans.Item2) + " aUEC");
            }

            return builder.Build();
        }


        public ulong GetBankStatusEmbedId()
        {
            if (bankData.BankEmbed < 1)
                return 0;
            else
                return bankData.BankEmbed;
        }

        public void WipeBank(DiscordGuild guild)
        {
            
            Psql.WipeBank(guild);
            Psql.WipeTransactions(guild);
        }

        public void SaveBankData()
        {
           Serialization.Serialize(typeof(BankData),bankData, BANK_DATA_PATH);
        }

        public void UpdateTransaction(BankTransaction trans)
        {
            
            Ranks ranks = new Ranks();
            Psql.UpdateUserTransaction(ranks.GetNickWithoutRank(trans.Member), trans.Amount, Psql.GetOrgId(trans.Guild.Name));
        }

        public async Task<BankTransaction> GetBankActionAsync(CommandContext ctx)
        {
            string[] args = Regex.Split(ctx.Message.Content, @"\s+");


            if(args.Length == 3)
            {
                BankTransaction transaction = new BankTransaction(args[1], ctx.Member, int.Parse(args[2]), ctx.Guild);
                return transaction;
            }
            else if(args.Length == 4)
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

        private string FormattedNumber(string amount)
        {
            return String.Format("{0:n0}", int.Parse(amount));
        }
    }
}