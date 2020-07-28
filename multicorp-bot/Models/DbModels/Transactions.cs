using System;
using System.Collections.Generic;

namespace multicorp_bot
{
    public partial class Transactions
    {
        public int TransactionId { get; set; }
        public int UserId { get; set; }
        public int? Amount { get; set; }
        public int? Merits { get; set; }

        public virtual Mcmember User { get; set; }
    }
}
