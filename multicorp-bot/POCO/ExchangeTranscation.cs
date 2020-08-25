using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace multicorp_bot.POCO
{
    public class ExchangeTranscation
    {
        public int Amount;
        public int Merits;
        public DiscordGuild Guild;

        public ExchangeTranscation(DiscordGuild guild, int amount = 0, int merits = 0)
        {
            Amount = amount;
            Merits = merits;
            Guild = guild;
        }
    }
}
