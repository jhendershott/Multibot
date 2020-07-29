using DSharpPlus.Entities;

namespace multicorp_bot.POCO
{
    public class BankTransaction
    {
        public string Action;
        public DiscordMember Member;
        public int Amount;
        public int Merits;
        public DiscordGuild Guild;

        public BankTransaction(string action, DiscordMember member, DiscordGuild guild, int amount = 0, int merits = 0)
        { 
            Action = action;
            Member = member;
            Amount = amount;
            Merits = merits;
            Guild = guild;
        }
    }
}
