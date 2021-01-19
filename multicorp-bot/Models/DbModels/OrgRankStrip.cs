using System;
using System.Collections.Generic;

namespace multicorp_bot
{
    public partial class OrgRankStrip
    {

        public int Id { get; set; }
        public string DiscordId { get; set; }
        public string OldNick { get; set; }
        public string NewNick { get; set; }

        public string OrgName { get; set; }
    }
}
