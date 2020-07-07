using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus.Entities;

namespace multicorp_bot.POCO
{
    public class BankTransaction
    {
        public string Action;
        public DiscordMember Member;
        public int Amount;
        public DiscordGuild Guild;

        public BankTransaction(string action, DiscordMember member, int amount, DiscordGuild guild)
        {
            Action = action;
            Member = member;
            Amount = amount;
            Guild = guild;
        }
    }
}
