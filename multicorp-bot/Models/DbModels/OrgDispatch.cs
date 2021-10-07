using System;
namespace multicorp_bot.Models.DbModels
{
    public class OrgDispatch
    {
        public OrgDispatch() { }

        public int OrgDispatchId { get; set; }
        public int OrgId { get; set; }
        public int DispatchType { get; set; }
    }
}
