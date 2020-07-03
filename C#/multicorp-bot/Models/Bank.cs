using DSharpPlus.Entities;
using multicorp_bot.Models;
using multicorp_bot.POCO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace multicorp_bot
{
    public class Bank
    {
        BankData bankData;
        const string BANK_DATA_PATH = "./bank.xml";
        public ulong BankEmbedId { set { bankData.BankEmbed = value; } }

        public Bank()
        {
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

        public void Withdraw(DiscordMember user, int amount)
        {
            try
            {
                bankData.Transactions.Add(new KeyValuePair<DiscordMember, int>(user, -amount));
                bankData.BankBalance -= amount;
                SaveBankData();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }

        public DiscordEmbed GetBankBalance()
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            builder.Title = "MultiCorp Bank";
            builder.Timestamp = DateTime.Now;
            builder.Description = $"Current Balance: {bankData.BankBalance} aUEC";

            foreach (var trans in bankData.Transactions.TakeLast(5).Reverse())
            {
                builder.AddField(trans.Key.Username, trans.Value + " aUEC");
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

        public void WipeBank()
        {
            bankData.BankBalance = 0;
            bankData.Transactions.Clear();
            bankData.Transactions.Clear();
            SaveBankData();
        }

        public void SaveBankData()
        {
           Serialization.Serialize(typeof(BankData),bankData, BANK_DATA_PATH);
        }
    }
}