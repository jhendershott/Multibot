using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace multicorp_bot
{
    public partial class WorkOrders
    {
        public string Description { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public int OrgId { get; set; }
        public int WorkOrderTypeId { get; set; }
        public string Location { get; set; }
        public bool isCompleted { get; set; }
        public int? FactionId { get; set; }
    }
}
