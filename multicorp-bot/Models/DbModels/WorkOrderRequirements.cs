using System;
using System.Collections.Generic;

namespace multicorp_bot
{
    public partial class WorkOrderRequirements
    {
        public int Id { get; set; }
        public int WorkOrderId { get; set; }
        public int TypeId { get; set; }
        public int Amount { get; set; }
    }
}
