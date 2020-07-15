using System;
using System.Collections.Generic;

namespace multicorp_bot
{
    public partial class Bank
    {
        public int AccountId { get; set; }
        public long? Balance { get; set; }
        public int? OrgId { get; set; }

        public virtual Orgs Org { get; set; }
    }
}
