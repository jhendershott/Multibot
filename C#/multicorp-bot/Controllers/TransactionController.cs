using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using multicorp_bot.POCO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace multicorp_bot.Controllers
{
    public class TransactionController
    {
        MultiBotDb MultiBotDb;
        public TransactionController()
        {
            MultiBotDb = new MultiBotDb();
        }

        public void AddTransaction(int userId, int amount)
        {
            var transContext = MultiBotDb.Transactions;
            var transItem = new Transactions()
            {
                UserId = userId,
                Amount = amount
            };

            transContext.Add(transItem);
        }

        public int? GetTransactionId(int? userId)
        {
            var transContext = MultiBotDb.Transactions;
            try
            {
                return transContext.Single(x => x.UserId == userId).TransactionId;
            }
            catch 
            {
                return null;
            }
            
        }

        public void UpdateTransaction(int? transId, int newValue)
        {
            var transContext = MultiBotDb.Transactions;
            var trans = transContext.Single(x => x.TransactionId == transId);
            trans.Amount = trans.Amount + newValue;
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
                        amount = trans.Amount
                    }              
                 ).Where(x => x.orgId == new OrgController().GetOrgId(guild)).OrderByDescending(x => x.amount).ToList();

            var transactions = new List<TransactionItem>();
            foreach (var item in data)
            {

                transactions.Add(new TransactionItem(item.memberName, item.orgId, item.amount.GetValueOrDefault()));
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
