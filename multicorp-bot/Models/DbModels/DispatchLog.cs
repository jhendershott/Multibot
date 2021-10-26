using System;
namespace multicorp_bot.Models.DbModels
{
    public class DispatchLog
    {
        public DispatchLog()
        {
        }

        public int Id { get; set; }
        public string RequestorName { get; set; }
        public string RequestorOrg { get; set; }
        public string AcceptorName { get; set; }
        public string AcceptorOrg { get; set; }


    }
}
