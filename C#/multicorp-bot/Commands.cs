using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using multicorp_bot.Controllers;
using multicorp_bot.POCO;

namespace multicorp_bot
{
    public class Commands
    {

        Ranks Ranks;
        BankController BankController;
        MemberController MemberController;
        TransactionController TransactionController;
        LoanController LoanController;
        OrgController OrgController;

        public Commands()
        {
            Ranks = new Ranks();
            BankController = new BankController();
            MemberController = new MemberController();
            TransactionController = new TransactionController();
            LoanController = new LoanController();
            OrgController = new OrgController();
            PermissionsHelper.LoadPermissions();
        }

        [Command("handle")]
        public async Task UpdateHandle(CommandContext ctx)
        {
            string[] args = Regex.Split(ctx.Message.Content, @"\s+");
            DiscordMember member = null;
            string newNick = null;
            if (args.Length == 2)
            {
                member = ctx.Member;
                newNick = Ranks.GetUpdatedNickname(member, args[1]);
            }
            else if (args.Length >= 3)
            {
                member = await ctx.Guild.GetMemberAsync(ctx.Message.MentionedUsers[0].Id);
                newNick = Ranks.GetUpdatedNickname(member, args[2]);
            }

            MemberController.UpdateMemberName(Ranks.GetNickWithoutRank(member), Ranks.GetNickWithoutRank(newNick), ctx.Guild);
            await member.ModifyAsync(nickname: newNick);
        }

        [Command("multibot-help")]
        public async Task Help(CommandContext ctx)
        {
            await ctx.RespondAsync("Which command would you like help with? Bank, Loans, Handle, Promotion or Wipe?");
            var interactivity = ctx.Client.GetInteractivityModule();
            var optMessage = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5));

            switch (optMessage.Message.Content.ToLower())
            {
                case "bank": await ctx.RespondAsync(embed: HelpController.BankEmbed());
                    break;
                case "loans":
                    await ctx.RespondAsync(embed: HelpController.LoanEmbed());
                    break;
                case "handle":
                    await ctx.RespondAsync(embed: HelpController.HandleEmbed());
                    break;
                case "promotion":
                    await ctx.RespondAsync(embed: HelpController.PromotionEmbed());
                    break;
                case "wipe":
                    await ctx.RespondAsync(embed: HelpController.WipeHelper());
                    break;
            }
        }


        [Command("check-requirements")]
        public async Task CheckRequirements(CommandContext ctx)
        {
            try
            {
                string missingRequirements = "";

                foreach (var item in Ranks.MilRanks)
                {
                    if (!ctx.Guild.Roles.Select(x => x.Name).Contains(item.RankName))
                        missingRequirements += $"Rank {item.RankName} missing\n";
                }

                await ctx.RespondAsync(missingRequirements);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }

        [Command("check")]
        public async Task Check(CommandContext ctx, DiscordUser user)
        {
            try
            {
                var level = PermissionsHelper.GetPermissionLevel(ctx.Guild, user);
                Console.WriteLine(level);
                await ctx.RespondAsync($"The permission level of {user.Mention} is: {level}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        [Command("set-role-level")]
        public async Task SetRoleLevel(CommandContext ctx, DiscordRole role, int level)
        {
            if (PermissionsHelper.GetPermissionLevel(ctx.Guild, ctx.User) < 2)
                return;

            try
            {
                PermissionsHelper.SetRolePermissionLevel(role, level);
                await ctx.RespondAsync($"{role.Mention} is now assigned to level {level}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        [Command("promote")]
        public async Task PromoteMember(CommandContext ctx)
        {
            if (!PermissionsHelper.CheckPermissions(ctx, Permissions.ManageRoles) && !PermissionsHelper.CheckPermissions(ctx, Permissions.ManageNicknames))
            {
                await ctx.RespondAsync("You can't do that you don't have the power!");
                return;
            }
             
            string congrats = $"Congratulations on your promotion :partying_face:";
            foreach (var user in ctx.Message.MentionedUsers)
            {
                DiscordMember member = await ctx.Guild.GetMemberAsync(user.Id);
                await Ranks.Promote(member);
                await member.ModifyAsync(Ranks.GetUpdatedNickname(member));
                congrats = congrats += $" {member.Mention}";
            }

            await ctx.Message.DeleteAsync();
            await ctx.RespondAsync(congrats);
        
        }

        [Command("demote")]
        public async Task DemoteMember(CommandContext ctx)
        {
            if (!PermissionsHelper.CheckPermissions(ctx, Permissions.ManageRoles) && !PermissionsHelper.CheckPermissions(ctx, Permissions.ManageNicknames))
            {
                await ctx.RespondAsync("You can't do that you don't have the power!");
                return;
            }

            string congrats = $"Oh no you've been demoted! What have you done :disappointed_relieved:";
            foreach (var user in ctx.Message.MentionedUsers)
            {
                DiscordMember member = await ctx.Guild.GetMemberAsync(user.Id);
                await Ranks.Demote(member);
                await member.ModifyAsync(Ranks.GetUpdatedNickname(member, -1));
                congrats = congrats += $" {member.Mention}";

            }
            await ctx.Message.DeleteAsync();
            await ctx.RespondAsync(congrats);
        }

        [Command("recruit")]
        public async Task RecruitMember(CommandContext ctx, DiscordMember member)
        {
            if (PermissionsHelper.GetPermissionLevel(ctx.Guild, ctx.User) < 1)
                return;
            await Ranks.Recruit(member);
            await ctx.RespondAsync($"Welcome on board {member.Mention} :alien:");
        }

        [Command("bank")]
        public async Task Bank(CommandContext ctx)
        {
            string[] args = Regex.Split(ctx.Message.Content, @"\s+");
            string newBalance;
            var interactivity = ctx.Client.GetInteractivityModule();
            BankTransaction transaction = null;
            

            try
            {
                switch (args[1].ToLower())
                {
                    case "deposit":
                        await ctx.RespondAsync("Please Make sure a Banker is online to assist you. Do you want to continue?");
                        var continueMsg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1));
                        if (!continueMsg.Message.Content.ToLower().Contains("yes"))
                        {
                            break;
                        }

                        transaction = await BankController.GetBankActionAsync(ctx);

                        var bankers = await GetMembersWithRolesAsync("Banker", ctx.Guild);

                        await ctx.RespondAsync("Waiting for Banker to Approve your request");
                        var confirmMsg = await interactivity.WaitForMessageAsync(xm => bankers.Contains((int)xm.Author.Id), TimeSpan.FromMinutes(10));
                        
                        if (confirmMsg.Message.Content.ToLower().Contains("yes")
                            || confirmMsg.Message.Content.ToLower().Contains("confirm")
                            || confirmMsg.Message.Content.ToLower().Contains("approve"))
                        {
                            newBalance = BankController.Deposit(transaction);
                            BankController.UpdateTransaction(transaction);

                            await ctx.RespondAsync($"Thank you for your contribution of {transaction.Amount}! The new bank balance is {newBalance}");
                        }
                        else
                        {
                            await ctx.RespondAsync("Please Try again when a banker is present");
                        }

                        break;
                    case "withdraw":
                        transaction = await BankController.GetBankActionAsync(ctx);
                        newBalance = BankController.Withdraw(transaction);
                        await ctx.RespondAsync($"You have successfully withdrawn {transaction.Amount}. The new bank balance is {newBalance}");
                        break;
                    case "balance":
                        var balanceembed = BankController.GetBankBalanceEmbed(ctx.Guild);
                        await ctx.RespondAsync(embed: balanceembed);
                        break;
                }

            }

            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        [Command("loan")]
        public async Task Loan(CommandContext ctx)
        {
            string[] args = Regex.Split(ctx.Message.Content, @"\s+");

            if (args.Length == 1)
            {
                await ctx.RespondAsync($"Options for loans is 'request', 'view', 'payment', 'fund', and 'complete'\n For Example .loan request");
            }
            else
            { 
                switch (args[1].ToLower())
                {
                    case "request":
                        await LoanRequest(ctx);
                        break;
                    case "view":
                        await LoanView(ctx);
                        break;
                    case "payment":
                        await LoanPayment(ctx);
                        break;
                    case "pay":
                        await LoanPayment(ctx);
                        break;
                    case "fund":
                        await LoanFund(ctx);
                        break;
                    case "complete":
                        await LoanComplete(ctx);
                        break;
                    //case "add": LoanController.AddLoan(ctx.Member, ctx.Guild, 50000, 1000);
                    //    break;
                    default:
                        await ctx.RespondAsync("Options for loans is 'request', 'view', 'payment', 'fund', and 'complete'");
                        break;
                }
            }

        }

        [Command("wipe-bank")]
        public async Task WipeBank(CommandContext ctx)
        {
            var interactivity = ctx.Client.GetInteractivityModule();
            await ctx.RespondAsync("Are you sure you want to continue? This Cannot be undone");
            var confirmMsg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1));
     
            if(confirmMsg.Message.Content.ToLower() == "yes")
            {
                BankController.WipeBank(ctx.Guild);
                TransactionController.WipeTransactions(ctx.Guild);
                LoanController.WipeLoans(ctx);

                await ctx.RespondAsync("your org balance and transactions have been set to 0. All Loans have been completed");
            }
        }

        //[Command("getid")]
        //public async Task GetId(CommandContext ctx)
        //{

        //    foreach(var item in ctx.Message.MentionedUsers)
        //    {
        //        var member = await ctx.Guild.GetMemberAsync(item.Id);
        //        Console.WriteLine($"{member.Nickname} - {item.Id} - {member.Id}");
        //    }
        //}

        private async Task LoanFund(CommandContext ctx)
        {
            await ctx.RespondAsync("Which Loan would you like to fund", embed: LoanController.GetWaitingLoansEmbed(ctx.Guild));

            var interactivity = ctx.Client.GetInteractivityModule();
            var loanIdMsg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1));
            var loan = await LoanController.FundLoan(loanIdMsg);

            await ctx.RespondAsync($"Congratulations " +
                $"{(await MemberController.GetDiscordMemberByMemberId(ctx ,loan.FunderId.GetValueOrDefault())).Mention}! \n" +
                $"{(await MemberController.GetDiscordMemberByMemberId(ctx ,loan.ApplicantId)).Mention} is willing to fund your loan!" +
                $" Reach out to them to receive your funds");
        }

        private async Task LoanComplete(CommandContext ctx)
        {
            await ctx.RespondAsync("Which Loan would you like to complete", embed: LoanController.GetFundedLoansEmbed(ctx.Guild));

            var interactivity = ctx.Client.GetInteractivityModule();
            var loanIdMsg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1));
            
            var loan = LoanController.CompleteLoan(int.Parse(loanIdMsg.Message.Content));

            await ctx.RespondAsync($"Congratulations " +
                $"{(await MemberController.GetDiscordMemberByMemberId(ctx, loan.FunderId.GetValueOrDefault())).Mention}! \n" +
                $"You've paid off your loan and you're debt free! For now :money_mouth:");
        }

        private async Task LoanView(CommandContext ctx)
        {
            await ctx.RespondAsync("Getting your Loan Information, please hold");
            await ctx.RespondAsync(embed: LoanController.GetLoanEmbed(ctx.Guild));
        }



        private async Task LoanPayment(CommandContext ctx)
        {
            await ctx.RespondAsync("Pulling up your loan info now.");
            try
            {
                int memberId = MemberController.GetMemberbyDcId(ctx.Member, ctx.Guild).UserId;
                var loan = LoanController.GetLoanByApplicantId(memberId);

                if (loan != null)
                {
                    var interactivity = ctx.Client.GetInteractivityModule();
                    await ctx.RespondAsync($"Your current balance is {loan.RemainingAmount} much of your loan would you like to repay");
                    var amountmsg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1));

                    var fundedMember = await ctx.Guild.GetMemberAsync(ulong.Parse(MemberController.GetMemberById(loan.FunderId.GetValueOrDefault()).DiscordId));
                    await ctx.RespondAsync($"{fundedMember.Mention} Please confirm this payment, you have 10 minutes to confirm");
                    var confirm = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == fundedMember.Id, TimeSpan.FromMinutes(10))).Message.Content;
                    if (confirm.Contains("yes") || confirm.Contains("confirm"))
                    {
                        LoanController.MakePayment(loan.LoanId, int.Parse(amountmsg.Message.Content));
                        await ctx.RespondAsync($"Payment has been confirmed: The new balance is {LoanController.GetLoanById(loan.LoanId).RemainingAmount}");
                    }
                }
                else
                {
                    await ctx.RespondAsync("Could not find loan");
                }
            } catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }

        private async Task<List<int>> GetMembersWithRolesAsync(string roleLevel, DiscordGuild guild)
        {
            var bankerRole = guild.Roles.FirstOrDefault(x => x.Name == roleLevel);
            var members = await guild.GetAllMembersAsync();
            List<int> bankersIds = new List<int>();
            foreach(var member in members)
            {
                if (member.Roles.Contains(bankerRole))
                {
                    bankersIds.Add((int)member.Id);
                }
            }
            return bankersIds;
        }
        private async Task LoanRequest(CommandContext ctx)
        {
            var interactivity = ctx.Client.GetInteractivityModule();
           
            await ctx.RespondAsync("Thank you for banking with MultiBot. \nI'll need some info to process your request. \n\nHow much are you looking to borrow?");
            var amountmsg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1));
            try
            {
                int amount = int.Parse(amountmsg.Message.Content);
                await ctx.RespondAsync("Excellent! Do you prefer your interested to be 'percentage' or 'flat' rate?");

                var typemsg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1));
                string type = "";
                switch (typemsg.Message.Content.ToLower())
                {
                    case string a when a.Contains("percentage"):
                        type = "percent";
                        break;
                    case string b when b.Contains("flat"):
                        type = "flat";
                        break;
                    default:
                        await ctx.RespondAsync("Sorry I didn't get that. Please start over, types must include 'flat' or 'percentage'.");
                        break;
                }

                if(type == "percent")
                {
                    await ctx.RespondAsync("What % interest are you offering. Please use whole numbers: e.g. 3");
                    var interest = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1));
                    
                    var interestamount = LoanController.CalculateInterest(amount, int.Parse(interest.Message.Content));
                    await ctx.RespondAsync("Please hold your application is being processed");
                    LoanController.AddLoan(ctx.Member, ctx.Guild, amount, interestamount);
                    await ctx.RespondAsync($"Your Loan of {amount} with a total repayment of {amount + interestamount} is waiting for funding");
                }
                else
                {
                    await ctx.RespondAsync("What amount interest are you offering. Please use whole numbers: e.g. 20000");
                    var interest = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1));
                    var interestAmount = int.Parse(interest.Message.Content);
                    await ctx.RespondAsync("Please hold your application is being processed");

                    try
                    {
                        LoanController.AddLoan(ctx.Member, ctx.Guild, amount, interestAmount);
                        await ctx.RespondAsync($"Your Loan of {amount} with a total repayment of {amount + interestAmount} is waiting for funding");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        await ctx.RespondAsync("I'm sorry, but an error has occured please notify your banker.");
                    }
                }

            }
            catch(Exception e)
            {
                Console.WriteLine(e);

                await ctx.RespondAsync("I'm sorry, but an error has occured please notify your banker.");
            }


        }
    }
}