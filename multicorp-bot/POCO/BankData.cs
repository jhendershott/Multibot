using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace multicorp_bot.POCO
{
    public class BankData
    {
        public List<KeyValuePair<DiscordMember, int>> Transactions { get; set; } = new List<KeyValuePair<DiscordMember, int>>();
        public int BankBalance { get; set; }
        public ulong BankEmbed { get; set; }
    }
}
