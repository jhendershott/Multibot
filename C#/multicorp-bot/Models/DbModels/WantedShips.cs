using System;
using System.Collections.Generic;

namespace multicorp_bot
{
    public partial class WantedShips
    {
        public int Id { get; set; }
        public int OrgId { get; set; }
        public string Name { get; set; }
        public int TotalPrice { get; set; }
        public int? RemainingPrice { get; set; }
        public string ImgUrl { get; set; }
    }
}
