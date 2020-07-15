using System;
using System.Collections.Generic;

namespace multicorp_bot
{
    public partial class Mcmember
    {
        public Mcmember()
        {
            Transactions = new HashSet<Transactions>();
        }

        public int UserId { get; set; }
        public string Username { get; set; }
        public int? OrgId { get; set; }
        public string DiscordId { get; set; }
        public long? Xp { get; set; }

        public virtual ICollection<Transactions> Transactions { get; set; }

    }
}
