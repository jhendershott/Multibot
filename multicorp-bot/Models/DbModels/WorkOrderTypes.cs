using System;
using System.Collections.Generic;

namespace multicorp_bot
{
    public partial class WorkOrderTypes
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double XpModifier { get; set; }
        public string ImgUrl { get; set; }
        public double CreditModifier { get; set; }
    }
}
