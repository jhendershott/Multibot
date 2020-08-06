using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using multicorp_bot.Helpers;
using multicorp_bot.POCO;

namespace multicorp_bot
{
    public class Commands
    {

        readonly Ranks Ranks;
        readonly BankController BankController;
        readonly MemberController MemberController;
        readonly TransactionController TransactionController;
        readonly LoanController LoanController;
        readonly FleetController FleetController;
        readonly OrgController OrgController;

        public Commands()
        {
            Ranks = new Ranks();
            BankController = new BankController();
            MemberController = new MemberController();
            TransactionController = new TransactionController();
            LoanController = new LoanController();
            FleetController = new FleetController();
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
            await ctx.RespondAsync("Which command would you like help with? Bank, Loans, Handle, Promotion, Fleet or Wipe?");
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
                case "fleet":
                    await ctx.RespondAsync(embed: HelpController.FleetEmbed());
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
            Tuple<string, string> newBalance;
            var interactivity = ctx.Client.GetInteractivityModule();
            BankTransaction transaction = null;
            var bankers = await GetMembersWithRolesAsync("Banker", ctx.Guild);


            try
            {
                switch (args[1].ToLower())
                {
                    case "deposit":
                        await ctx.RespondAsync("Please Make sure a Banker is online to assist you. Do you want to continue?");
                        var continueMsg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1));
                        if (!continueMsg.Message.Content.ToLower().Contains("yes"))
                        {
                            await ctx.RespondAsync("Thank you and have a great day");
                            break;
                        }
                   
                        await ctx.RespondAsync("Are you depositing Credits or Merits?");

                        var currency = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1));

                        bool isCredit = true;
                        if (currency.Message.Content.ToLower().Contains("credit"))
                        {
                            transaction = await BankController.GetBankActionAsync(ctx);
                        }
                        else if (currency.Message.Content.ToLower().Contains("merit"))
                        {
                            transaction = await BankController.GetBankActionAsync(ctx, false);
                            isCredit = false;
                        }
                        else
                        {
                            await ctx.RespondAsync("Transaction types can only include Credit or Merit, please start over");
                            return;
                        }

                        await ctx.RespondAsync("Waiting for Banker to Approve your request");
                        var confirmMsg = await interactivity.WaitForMessageAsync(xm => bankers.Contains((int)xm.Author.Id), TimeSpan.FromMinutes(10));
                        
                        if (confirmMsg.Message.Content.ToLower().Contains("yes")
                            || confirmMsg.Message.Content.ToLower().Contains("confirm")
                            || confirmMsg.Message.Content.ToLower().Contains("approve"))
                        {
                                newBalance = BankController.Deposit(transaction);
                                BankController.UpdateTransaction(transaction);
                                MemberController.UpdateExperiencePoints("credits", transaction);

                            if (isCredit)
                            {
                                await ctx.RespondAsync($"Thank you for your contribution of {transaction.Amount}! The new bank balance is {newBalance.Item1} aUEC");
                                MemberController.UpdateExperiencePoints("credits", transaction);
                            }
                            else{
                                await ctx.RespondAsync($"Thank you for your contribution of {transaction.Merits}! The new bank balance is {newBalance.Item2} Merits");
                                MemberController.UpdateExperiencePoints("merits", transaction);
                            }
                              
                                
                        }
                        else
                        {
                            await ctx.RespondAsync("Please Try again when a banker is present");
                        }

                        break;
                    case "withdraw":

                        await ctx.RespondAsync("Are you depositing Credits or Merits?");
                        currency = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1));

                        isCredit = true;
                        if (currency.Message.Content.ToLower().Contains("credit"))
                        {
                            transaction = await BankController.GetBankActionAsync(ctx);
                        }
                        else if (currency.Message.Content.ToLower().Contains("merit"))
                        {
                            transaction = await BankController.GetBankActionAsync(ctx, false);
                            isCredit = false;
                        }
                        newBalance = BankController.Withdraw(transaction);
                        if (isCredit)
                        {
                            await ctx.RespondAsync($"You have successfully withdrawn {transaction.Amount}! The new bank balance is {newBalance.Item1} aUEC");
                        }
                        else
                        {
                            await ctx.RespondAsync($"You have successfully withdraw {transaction.Merits}! The new bank balance is {newBalance.Item2} Merits");
                        }
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

        [Command("fleet")]
        public async Task Fleet(CommandContext ctx, string arg)
        {
            switch(arg.ToLower()){
                case "view": await ctx.RespondAsync(embed: new FleetController().GetFleetRequests(ctx.Guild));
                    break;
                case "request":
                    await FleetRequest(ctx);
                    break;
                case "fund":
                    await FundFleet(ctx);
                    break;
                case "complete": var completed = FleetController.CompleteFleetRequest(ctx.Guild);
                    await ctx.RespondAsync($"{completed} requests have been marked complete");
                    break;
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

        private async Task FleetRequest(CommandContext ctx)
        {
            await ctx.RespondAsync("What is the Make and Model of the ship you're requesting");
            var interactivity = ctx.Client.GetInteractivityModule();
            var item = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1))).Message.Content;

            await ctx.RespondAsync("What is the price of the ship in aUEC");
            var price = int.Parse((await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1))).Message.Content);
            await ctx.RespondAsync("Please provide an image url of the ship you're requestion");
            var image = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5))).Message.Content;
            try
            {
                FleetController.AddFleetRequest(item, price, image, ctx.Guild);
                await ctx.RespondAsync("Your request has been logged", embed: FleetController.GetFleetRequests(ctx.Guild));
            }
            catch(Exception e)
            {
                await ctx.RespondAsync("Something went wrong with your request");
                Console.WriteLine(e);
            }
        }

        private async Task FundFleet(CommandContext ctx)
        {
            await ctx.RespondAsync("What is the ID of the ship you would like to fun");
            var interactivity = ctx.Client.GetInteractivityModule();
            var item = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1))).Message.Content;
            await ctx.RespondAsync("How many credits you put towards the ship");
            var credits = int.Parse((await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1))).Message.Content);

            await ctx.RespondAsync("Waiting for Banker to confirm the transfer");
            var bankers = await GetMembersWithRolesAsync("Banker", ctx.Guild);
            var confirmMsg = await interactivity.WaitForMessageAsync(xm => bankers.Contains((int)xm.Author.Id), TimeSpan.FromMinutes(10));
            if (confirmMsg.Message.Content.ToLower().Contains("yes")
                || confirmMsg.Message.Content.ToLower().Contains("confirm")
                || confirmMsg.Message.Content.ToLower().Contains("approve"))
            {
                BankTransaction trans = new BankTransaction("deposit", ctx.Member, ctx.Guild, credits);
                BankController.Deposit(trans);
                BankController.UpdateTransaction(trans);
                var xp = MemberController.UpdateExperiencePoints("credits for ships" ,trans);
                FleetController.UpdateFleetItemAmount(int.Parse(item), credits);
                await ctx.RespondAsync($"Your funds have been accepted and you've been credited the transaction.\n Your org experience is now {FormatHelpers.FormattedNumber(xp.ToString())}");
            }

        }

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