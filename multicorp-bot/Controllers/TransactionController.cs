using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore.Internal;
using multicorp_bot.POCO;
using System.Collections.Generic;
using System.Linq;


namespace multicorp_bot.Controllers
{
    public class TransactionController
    {
        MultiBotDb MultiBotDb;
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
            var data = MultiBotDb.Transactions
                .Join(
                    MultiBotDb.Mcmember,
                    trans => trans.UserId,
                    mem => mem.UserId,
                    (trans, mem) => new
                    {
                        memberName = mem.Username,
                        orgId = mem.OrgId.GetValueOrDefault(),
                        amount = trans.Amount,
                        merits = trans.Merits
                    }              
                 ).Where(x => x.orgId == new OrgController().GetOrgId(guild)).OrderByDescending(x => x.amount).ToList();

            var transactions = new List<TransactionItem>();

            for (int i = 0; i <= 2; i++)
            {
                transactions.Add(new TransactionItem(data[i].memberName, data[i].orgId, data[i].amount.GetValueOrDefault(), data[i].merits.GetValueOrDefault()));
            }

            return transactions;

                
            //return transContext.OrderByDescending(x => x.Amount).Take(5).ToList();
        }

        public List<TransactionItem> GetTopMeritTransactions(DiscordGuild guild)
        {
            var orgId = new OrgController().GetOrgId(guild);
            var data = MultiBotDb.Transactions
                .Join(
                    MultiBotDb.Mcmember,
                    trans => trans.UserId,
                    mem => mem.UserId,
                    (trans, mem) => new
                    {
                        memberName = mem.Username,
                        orgId = mem.OrgId.GetValueOrDefault(),
                        amount = trans.Amount,
                        merits = trans.Merits
                        
                    }
                 ).Where(x => x.orgId == new OrgController().GetOrgId(guild) && x.amount != 0 && x.merits != 0).OrderByDescending(x => x.amount).ToList();

            var transactions = new List<TransactionItem>();
            for (int i = 0; i <= 2; i++)
            {
                transactions.Add(new TransactionItem(data[i].memberName, data[i].orgId, data[i].amount.GetValueOrDefault(), data[i].merits.GetValueOrDefault()));
            }

            return transactions;


            //return transContext.OrderByDescending(x => x.Amount).Take(5).ToList();
        }


        public void WipeTransactions(DiscordGuild guild)
        {
            var transContext = MultiBotDb.Transactions;
            OrgController orgC = new OrgController();
            var memCont = new MemberController();
            var bankItems = transContext.ToList();

            var users = memCont.GetMembersByOrgId(orgC.GetOrgId(guild));

            foreach(var user in users){
                bankItems.Single(x => x.UserId == user.UserId).Amount = 0;   
            }
            transContext.UpdateRange(bankItems);
            MultiBotDb.SaveChanges();
        }
    }
}
