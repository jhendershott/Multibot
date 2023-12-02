using System;
namespace multicorp_bot.Models.DbModels
{
    public class Expenses
    {
        public Expenses()
        {
        }

        public int Id { get; set; }
        public int OrgId { get; set; }
        public string Name { get; set; }
        public long Amount { get; set; }
        public long Remaining { get; set; }
        public int Period { get; set; }
        public int? NumPeriods { get; set; }
    }
}
