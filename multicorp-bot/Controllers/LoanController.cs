using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using multicorp_bot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace multicorp_bot.Controllers
{
    public class LoanController
    {
        MultiBotDb MultiBotDb;
        TelemetryHelper tHelper = new TelemetryHelper();
        public LoanController()
        {
            MultiBotDb = new MultiBotDb();
        }

        public int CalculateInterest(int amount, int percentage)
        {
            return (int)Math.Round((double)(amount / 100) * percentage);
        }

        private int GetHighestLoanId()
        {
            var listLoans = MultiBotDb.Loans.ToList();
            if(listLoans.Count == 0)
            {
                return 1;
            }
            return listLoans.OrderByDescending(x => x.LoanId).First().LoanId;
        }

        public void AddLoan(DiscordMember member, DiscordGuild guild, int totalAmount, int interestAmount)
        {
            var loanCtx = MultiBotDb.Loans;
            try
            {
                int orgId = new OrgController().GetOrgId(guild);
                var loan = new Loans()
                {
                    LoanId = GetHighestLoanId() + 1,
                    ApplicantId = new MemberController().GetMemberbyDcId(member, guild).UserId,
                    RequestedAmount = totalAmount,
                    RemainingAmount = totalAmount + interestAmount,
                    InterestAmount = interestAmount,
                    Status = "Waiting To Be Funded",
                    OrgId = orgId

                };
                loanCtx.Add(loan);
                MultiBotDb.SaveChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public DiscordEmbed GetLoanEmbed(DiscordGuild guild)
        {
            try
            {
                var loanCtx = MultiBotDb.Loans;
                int orgId = new OrgController().GetOrgId(guild);
                var loanList = loanCtx.AsQueryable().AsQueryable().Where(x => x.OrgId == orgId && x.IsCompleted == 0).ToList();
                var memberController = new MemberController();

                DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
                builder.Title = $"{guild.Name} Loans";
                builder.Timestamp = DateTime.Now;

                builder.Description = $"Here is a list of all of your Loans that are outstanding or waiting for funding! \n\u200b";


                builder.AddField("Status: Not Funded", "Waiting To Be Funded").WithColor(DiscordColor.Green);

                foreach (var item in loanList.AsQueryable().Where(x => x.Status == "Waiting To Be Funded"))
                {
                    builder.AddField($"Loan ID: {item.LoanId} \nApplicant: {memberController.GetMemberById(item.ApplicantId).Username}",
                        $"Asking for ${FormatHelpers.FormattedNumber(item.RequestedAmount.ToString())} for interest payment of " +
                        $"${FormatHelpers.FormattedNumber(item.InterestAmount.ToString())}")
                        .WithColor(DiscordColor.Green);
                }

                builder.AddField("\u200b\n Status: Funded", "Awaiting Repayment").WithColor(DiscordColor.Green);

                foreach (var item in loanList.AsQueryable().Where(x => x.Status == "Funded"))
                {
                    builder.AddField($"Loan ID: {item.LoanId} \nApplicant: {memberController.GetMemberById(item.ApplicantId).Username}" +
                        $"\nFunded by {memberController.GetMemberById(item.FunderId.GetValueOrDefault()).Username}",
                        $"Total Loan: ${FormatHelpers.FormattedNumber((item.RequestedAmount + item.InterestAmount).ToString())} \n" +
                        $"Remaining Amount:  ${FormatHelpers.FormattedNumber(item.RemainingAmount.ToString())}")
                        .WithColor(DiscordColor.Red);
                }

                return builder.Build();
            }
            catch (Exception e)
            {
                tHelper.LogException($"Method: GetLoanEmbed; Org: {guild.Name};", e);
                Console.WriteLine(e);
                return null;
            }
        }

        public List<Loans> GetWaitingLoans(DiscordGuild guild)
        {
            var loanList = MultiBotDb.Loans.AsQueryable().Where(x =>
            x.OrgId == new OrgController().GetOrgId(guild)
            && x.IsCompleted == 0
            && x.Status == "Waiting To Be Funded").ToList();

            return loanList;
        }

        public List<Loans> GetFundedLoans(DiscordGuild guild)
        {
            var loanList = MultiBotDb.Loans.AsQueryable().Where(x =>
            x.OrgId == new OrgController().GetOrgId(guild)
            && x.IsCompleted == 0
            && x.Status == "Funded").ToList();

            return loanList;
        }


        public DiscordEmbed GetWaitingLoansEmbed(DiscordGuild guild)
        {
            var loans = GetWaitingLoans(guild);
            var builder = new DiscordEmbedBuilder();
            builder.Title = "Loans Waiting for Funding";
            builder.Description = "Please provide the loan ID you want to fund";
            foreach (var loan in loans)
            {
                builder.AddField($"ID: {loan.LoanId} - Applicant: {new MemberController().GetMemberById(loan.ApplicantId).Username}"
                    , $"Requesting ${FormatHelpers.FormattedNumber(loan.RequestedAmount.ToString())} \n" +
                    $"They are willing to pay ${FormatHelpers.FormattedNumber(loan.InterestAmount.ToString())} in interest\n" +
                    $"The Total Repayment will be ${FormatHelpers.FormattedNumber(loan.RemainingAmount.ToString())}");
            }

            return builder.Build();

        }

        public DiscordEmbed GetFundedLoansEmbed(DiscordGuild guild)
        {
            var loans = GetFundedLoans(guild);
            var builder = new DiscordEmbedBuilder();
            builder.Title = "Funded Loans";
            builder.Description = "To Complete a loan ensure you have paid the final amount, then respond with the Loan Id";
            foreach (var loan in loans)
            {
                builder.AddField($"ID: {loan.LoanId} - Applicant: {new MemberController().GetMemberById(loan.ApplicantId).Username}"
                    , $"Requesting ${FormatHelpers.FormattedNumber(loan.RequestedAmount.ToString())} \n" +
                    $"They are willing to pay ${FormatHelpers.FormattedNumber(loan.InterestAmount.ToString())} in interest\n" +
                    $"The Total Repayment will be ${FormatHelpers.FormattedNumber(loan.RemainingAmount.ToString())}");
            }

            return builder.Build();
        }

        public async Task<Loans> FundLoan(CommandContext ctx, DiscordMember member, DiscordGuild guild, DiscordMessage response, bool isBank = false)
        {
            try
            {
                var loan = GetLoanById(int.Parse(response.Content));
                if (isBank)
                {
                    loan.FunderId = 0;
                }
                else
                {
                    loan.FunderId = new MemberController().GetMemberbyDcId(member, guild).UserId;
                }

                loan.Status = "Funded";
                await MultiBotDb.SaveChangesAsync();

                return loan;
            } catch( Exception e)
            {
                Console.WriteLine(e);
            }

            return null;
        }

        public List<Loans> GetLoanByApplicantId(int appId)
        {
            return MultiBotDb.Loans.AsQueryable().Where(x => x.ApplicantId == appId && x.IsCompleted == 0).ToList();
        }

        public Loans GetLoanById(int id)
        {
            return MultiBotDb.Loans.Single(x => x.LoanId == id);
        }

        public int MakePayment(int loanId, int paymentAmount)
        {
            var loan = MultiBotDb.Loans.Single(x => x.LoanId == loanId);
            int newbalance = (int)loan.RemainingAmount - paymentAmount;
            loan.RemainingAmount = newbalance;
            MultiBotDb.SaveChanges();
            return newbalance;
        }

        public void WipeLoans(CommandContext ctx)
        {
            var loans = MultiBotDb.Loans.AsQueryable().Where(x => x.IsCompleted == 0 && x.OrgId == new OrgController().GetOrgId(ctx.Guild));
            foreach(var loan in loans)
            {
                loan.IsCompleted = 1;
            }
            MultiBotDb.SaveChangesAsync();
        }

        public Loans CompleteLoan(int loanId)
        {
            var loan = MultiBotDb.Loans.Single(x => x.LoanId == loanId);
            loan.IsCompleted = 1;
            loan.Status = "Completed";
            MultiBotDb.SaveChangesAsync();

            return loan;
        }

    }
}
