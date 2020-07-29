using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace multicorp_bot
{
    public partial class Loans
    {
        public int LoanId { get; set; }
        public int ApplicantId { get; set; }
        public string Status { get; set; }
        public int? FunderId { get; set; }
        public long? OrgId { get; set; }
        public long? RequestedAmount { get; set; }
        public long? InterestAmount { get; set; }
        public long? RemainingAmount { get; set; }
        public int IsCompleted { get; set; }

        public virtual Mcmember Applicant { get; set; }
        public virtual Mcmember Funder { get; set; }
    }
}
