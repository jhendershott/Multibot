using System;
using System.Collections.Generic;
using System.Text;

namespace multicorp_bot.POCO
{
    public class TransactionItem
    {
        public string MemberName;
        public int OrgId;
        public int Amount;
        public int Merits;

        public TransactionItem(string name, int orgid, int amount, int merits)
        {
            MemberName = name;
            OrgId = orgid;
            Amount = amount;
            Merits = merits;
        }
    }
}
