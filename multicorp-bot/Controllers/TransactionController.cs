using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore.Internal;
using multicorp_bot.POCO;
using multicorp_bot.Helpers;
using System.Collections.Generic;
using System.Linq;


namespace multicorp_bot.Controllers
{
    public class TransactionController
    {
        MultiBotDb MultiBotDb;
        TelemetryHelper tHelper = new TelemetryHelper();
        public TransactionController()
        {
            MultiBotDb = new MultiBotDb();
        }

        public int AddTransaction(int? userId, int amount = 0, int merits = 0)
        {
            var transContext = MultiBotDb.Transactions;
            var transItem = new Transactions()
            {
                UserId = userId.GetValueOrDefault(),
                Amount = amount,
                Merits = merits
            };

            transContext.Add(transItem);
            MultiBotDb.SaveChanges();
            return GetTransactionId(userId);
        }

        public int GetTransactionId(int? userId)
        {
            var transContext = MultiBotDb.Transactions;
            try
            {
                return transContext.Single(x => x.UserId == userId).TransactionId;
            }
            catch
            {
                return AddTransaction(userId.GetValueOrDefault(), 0);
            }

        }

        public void UpdateTransaction(int? transId, BankTransaction transaction)
        {
            var transContext = MultiBotDb.Transactions;
            var trans = transContext.Single(x => x.TransactionId == transId);
            trans.Amount = trans.Amount + transaction.Amount;
            trans.Merits = trans.Merits + transaction.Merits;
            transContext.Update(trans);
            MultiBotDb.SaveChanges();
        }

        public int? GetTransactionValue(int userId)
        {
            var transContext = MultiBotDb.Transactions;
            return transContext.Single(x => x.UserId == userId).Amount;
        }

        public List<TransactionItem> GetTopTransactions(DiscordGuild guild)
        {
            var data = MultiBotDb.Transactions.AsQueryable()
                .Join(
                    MultiBotDb.Member,
                    trans => trans.UserId,
                    mem => mem.UserId,
                    (trans, mem) => new
                    {
                        memberName = mem.Username,
                        orgId = mem.OrgId.GetValueOrDefault(),
                        amount = trans.Amount,
                        merits = trans.Merits
                    }
                 ).Where(x => x.orgId == new OrgController().GetOrgId(guild) && x.amount != 0).OrderByDescending(x => x.amount).ToList();

            var transactions = new List<TransactionItem>();

            var length = data.Count;

            if (length < 3)
            {
                for (int i = 0; i < length; i++)
                {
                    transactions.Add(new TransactionItem(data[i].memberName, data[i].orgId, data[i].amount.GetValueOrDefault(), data[i].merits.GetValueOrDefault()));
                }
            }
            else
            {
                for (int i = 0; i <= 2; i++)
                {
                    transactions.Add(new TransactionItem(data[i].memberName, data[i].orgId, data[i].amount.GetValueOrDefault(), data[i].merits.GetValueOrDefault()));
                }
            }


            return transactions;
        }

        public List<TransactionItem> GetTopMeritTransactions(DiscordGuild guild)
        {
            var data = MultiBotDb.Transactions.AsQueryable()
                .Join(
                    MultiBotDb.Member,
                    trans => trans.UserId,
                    mem => mem.UserId,
                    (trans, mem) => new
                    {
                        memberName = mem.Username,
                        orgId = mem.OrgId.GetValueOrDefault(),
                        amount = trans.Amount,
                        merits = trans.Merits

                    }
                 ).Where(x => x.orgId == new OrgController().GetOrgId(guild) && x.merits != 0).OrderByDescending(x => x.merits).ToList();

            var transactions = new List<TransactionItem>();

            var length = data.Count;

            if (length < 3)
            {
                for (int i = 0; i < length; i++)
                {
                    transactions.Add(new TransactionItem(data[i].memberName, data[i].orgId, data[i].amount.GetValueOrDefault(), data[i].merits.GetValueOrDefault()));
                }
            }
            else
            {
                for (int i = 0; i <= 2; i++)
                {
                    transactions.Add(new TransactionItem(data[i].memberName, data[i].orgId, data[i].amount.GetValueOrDefault(), data[i].merits.GetValueOrDefault()));
                }
            }

            return transactions;
        }


        public void WipeTransactions(DiscordGuild guild)
        {
            var transContext = MultiBotDb.Transactions;
            OrgController orgC = new OrgController();
            var memCont = new MemberController();
            var bankItems = transContext.ToList();

            var users = memCont.GetMembersByOrgId(orgC.GetOrgId(guild));

            foreach (var user in users)
            {
                bankItems.Single(x => x.UserId == user.UserId).Amount = 0;
                bankItems.Single(x => x.UserId == user.UserId).Merits = 0;
            }
            transContext.UpdateRange(bankItems);
            MultiBotDb.SaveChanges();
        }

        public DiscordEmbed GetContributions(DiscordGuild guild, int? max = null)
        {
            var data = MultiBotDb.Transactions.AsQueryable()
                .Join(
                    MultiBotDb.Member,
                    trans => trans.UserId,
                    mem => mem.UserId,
                    (trans, mem) => new
                    {
                        memberName = mem.Username,
                        orgId = mem.OrgId.GetValueOrDefault(),
                        amount = trans.Amount,
                        merits = trans.Merits
                    }
                 ).Where(x => x.orgId == new OrgController().GetOrgId(guild) && (x.amount != 0 || x.merits != 0)).OrderByDescending(x => x.amount).ToList();

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();

            if (max == null)
            {
                builder.Title = $"{guild.Name} - All Contributions";
                foreach (var t in data)
                {
                    builder.AddField(t.memberName, $"**CREDITS:** {FormatHelpers.FormattedNumber(t.amount.ToString())} " +
                        $"\n**Merits:** {FormatHelpers.FormattedNumber(t.merits.ToString())}");
                }
            }
            else
            {
                builder.Title = $"{guild.Name} - Top {max} Contributions";
                for (int i = 0; i < max; i++)
                {
                    var t = data[i];
                    builder.AddField(t.memberName, $"**CREDITS:** {FormatHelpers.FormattedNumber(t.amount.ToString())} " +
                        $"\n**Merits:** {FormatHelpers.FormattedNumber(t.merits.ToString())}");
                }
            }


            return builder.Build();
        }


        public string GetContributions(DiscordGuild guild, DiscordMember dMember)
        {
            var data = MultiBotDb.Transactions.AsQueryable()
                .Join(
                    MultiBotDb.Member,
                    trans => trans.UserId,
                    mem => mem.UserId,
                    (trans, mem) => new
                    {
                        memberName = mem.Username,
                        discordID = mem.DiscordId,
                        orgId = mem.OrgId.GetValueOrDefault(),
                        amount = trans.Amount,
                        merits = trans.Merits,
                        xp = mem.Xp,
                    }
                 ).FirstOrDefault(x => x.orgId == new OrgController().GetOrgId(guild) && (x.amount != 0  || x.merits != 0)&& x.discordID == dMember.Id.ToString());

            if (data == null)
            {
                return $"Could not find Transactions for **{ dMember.Nickname ?? dMember.DisplayName}**";
            }
            else
            {
                return $"**Contribution by User:** {dMember.Nickname ?? dMember.DisplayName} \n " +
                $"**CREDITS:** {FormatHelpers.FormattedNumber(data.amount.ToString())}" +
                $"\n**MERITS:** {FormatHelpers.FormattedNumber(data.merits.ToString())}" +
                $"\n**ORG XP:** {FormatHelpers.FormattedNumber(data.xp.ToString())}";
            }
            
           
        }
    }
}
