using System;
using System.Collections.Generic;

namespace multicorp_bot
{
    public partial class Orgs
    {
        public Orgs()
        {
            Bank = new HashSet<Bank>();
        }

        public string OrgName { get; set; }
        public int Id { get; set; }

        public virtual ICollection<Bank> Bank { get; set; }
    }
}
