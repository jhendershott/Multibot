using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
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
        
        readonly MemberController MemberController;
        readonly TransactionController TransactionController;
        readonly LoanController LoanController;
        readonly FleetController FleetController;
        readonly OrgController OrgController;

        public Commands()
        {
            Ranks = new Ranks();
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
            BankController BankController = new BankController();
            string[] args = Regex.Split(ctx.Message.Content, @"\s+");
            Tuple<string, string> newBalance;
            var interactivity = ctx.Client.GetInteractivityModule();
            BankTransaction transaction = null;
            var bankers = await GetMembersWithRolesAsync("Banker", ctx.Guild);
            bool isCredit = true;

            try
            {
                switch (args[1].ToLower())
                {
                    case "deposit":

                        if (!bankers.Contains(ctx.Member.Id)){

                            var confirmation = await ctx.RespondAsync("Please Make sure a Banker is online to assist you. Do you want to continue?");
                            var confirmEmojis = ConfirmEmojis(ctx);
                            await confirmation.CreateReactionAsync(confirmEmojis[0]);
                            await confirmation.CreateReactionAsync(confirmEmojis[1]);
                            Thread.Sleep(500);
                            var continueMsg = await interactivity.WaitForMessageReactionAsync(r => r == confirmEmojis[0] || r == confirmEmojis[1], confirmation, timeoutoverride: TimeSpan.FromMinutes(1));

                            try
                            {
                                if (continueMsg.Emoji.Name != "✅")
                                {
                                    await continueMsg.Message.DeleteAsync();
                                    await ctx.RespondAsync("Please try again when you're ready");
                                    break;
                                }
                            }
                            catch (Exception e)
                            {
                                await continueMsg.Message.DeleteAsync();
                                await ctx.RespondAsync("Please try again when you're ready");
                                break;
                            }

                            if (!ctx.Message.Content.ToLower().Contains("merit") && !ctx.Message.Content.ToLower().Contains("credit"))
                            {
                                var cred = await ctx.RespondAsync("Are you depositing Credits or Merits?");
                                var credEmojis = ConfirmEmojis(ctx, "credit");
                                await cred.CreateReactionAsync(credEmojis[0]);
                                await cred.CreateReactionAsync(credEmojis[1]);
                                Thread.Sleep(500);

                                var creditmsg = await interactivity.WaitForMessageReactionAsync(r => r == credEmojis[0] || r == credEmojis[1], cred, timeoutoverride: TimeSpan.FromMinutes(1));

                                try
                                {
                                    if (creditmsg.Emoji.Name == "💰")
                                        isCredit = true;

                                    else if (creditmsg.Emoji.Name == "🎖")
                                        isCredit = false;
                                }
                                catch (Exception e)
                                {
                                    await ctx.RespondAsync("Please confirm Credits or Merits by clicking the appropriate reaction");
                                    break;
                                }
                            }
                            else if (ctx.Message.Content.ToLower().Contains("merit"))
                            {
                                isCredit = false;
                            }

                            if (isCredit)
                            {
                                transaction = await BankController.GetBankActionAsync(ctx);
                            }
                            else
                            {
                                transaction = await BankController.GetBankActionAsync(ctx, false);
                                isCredit = false;
                            }

                            var approval = await ctx.RespondAsync("Waiting for Banker to Approve your request");
                            await approval.CreateReactionAsync(confirmEmojis[0]);
                            await approval.CreateReactionAsync(confirmEmojis[1]);
                            Thread.Sleep(500);
                            var confirmMsg = await interactivity.WaitForMessageReactionAsync(r => r == confirmEmojis[0] || r == confirmEmojis[1], approval, timeoutoverride: TimeSpan.FromMinutes(20));
                            try
                            {
                                if (confirmMsg.Emoji.Name == "✅" && bankers.Contains(confirmMsg.User.Id))
                                {
                                    newBalance = BankController.Deposit(transaction);
                                    BankController.UpdateTransaction(transaction);
                                    MemberController.UpdateExperiencePoints("credits", transaction);

                                    if (isCredit)
                                    {
                                        if (transaction.Member != ctx.Member)
                                        {
                                            await ctx.RespondAsync($"Thank you for your {transaction.Member.Mention} contribution of {transaction.Amount}! The new bank balance is {newBalance.Item1} aUEC");
                                        }
                                        else
                                        {
                                            await ctx.RespondAsync($"Thank you for your contribution of {transaction.Amount}! The new bank balance is {newBalance.Item1} aUEC");
                                        }

                                        MemberController.UpdateExperiencePoints("credits", transaction);
                                    }
                                    else
                                    {
                                        if (transaction.Member != ctx.Member)
                                        {
                                            await ctx.RespondAsync($"Thank you for your {transaction.Member.Mention} contribution of {transaction.Merits}! The new bank balance is {newBalance.Item2} Merits");
                                        }
                                        else
                                        {
                                            await ctx.RespondAsync($"Thank you for your contribution of {transaction.Merits}! The new bank balance is {newBalance.Item2} Merits");
                                        }
                                        MemberController.UpdateExperiencePoints("merits", transaction);
                                    }
                                }
                                else if (!bankers.Contains(confirmMsg.User.Id))
                                {
                                    await ctx.RespondAsync("Looks like someone who isn't a banker attempted to approve the transactions. " +
                                        "Only bankers can approve transactions");
                                }

                            }
                            catch (Exception e)
                            {
                                await continueMsg.Message.DeleteAsync();
                                await ctx.RespondAsync("Either there was no confirmation or there was an error, please try again when a Banker is available to assist you");
                                break;
                            }
                        }
                        else
                        {
                            if (!ctx.Message.Content.ToLower().Contains("merit") && !ctx.Message.Content.ToLower().Contains("credit"))
                            {
                                var cred = await ctx.RespondAsync("Are you depositing Credits or Merits?");
                                var credEmojis = ConfirmEmojis(ctx, "credit");
                                await cred.CreateReactionAsync(credEmojis[0]);
                                await cred.CreateReactionAsync(credEmojis[1]);
                                Thread.Sleep(500);

                                var creditmsg = await interactivity.WaitForMessageReactionAsync(r => r == credEmojis[0] || r == credEmojis[1], cred, timeoutoverride: TimeSpan.FromMinutes(1));

                                try
                                {
                                    if (creditmsg.Emoji.Name == "💰")
                                        isCredit = true;

                                    else if (creditmsg.Emoji.Name == "🎖")
                                        isCredit = false;
                                }
                                catch (Exception e)
                                {
                                    await ctx.RespondAsync("Please confirm Credits or Merits by clicking the appropriate reaction");
                                    break;
                                }
                            }
                            else if (ctx.Message.Content.ToLower().Contains("merit"))
                            {
                                isCredit = false;
                            }

                            if (isCredit)
                            {
                                transaction = await BankController.GetBankActionAsync(ctx);
                            }
                            else
                            {
                                transaction = await BankController.GetBankActionAsync(ctx, false);
                                isCredit = false;
                            }
                            newBalance = BankController.Deposit(transaction);
                            BankController.UpdateTransaction(transaction);
                            MemberController.UpdateExperiencePoints("credits", transaction);

                            if (isCredit)
                            {
                                if (transaction.Member != ctx.Member)
                                {
                                    await ctx.RespondAsync($"Thank you for your {transaction.Member.Mention} contribution of {transaction.Amount}! The new bank balance is {newBalance.Item1} aUEC");
                                }
                                else
                                {
                                    await ctx.RespondAsync($"Thank you for your contribution of {transaction.Amount}! The new bank balance is {newBalance.Item1} aUEC");
                                }

                                MemberController.UpdateExperiencePoints("credits", transaction);
                            }
                            else
                            {
                                if (transaction.Member != ctx.Member)
                                {
                                    await ctx.RespondAsync($"Thank you for your {transaction.Member.Mention} contribution of {transaction.Merits}! The new bank balance is {newBalance.Item2} Merits");
                                }
                                else
                                {
                                    await ctx.RespondAsync($"Thank you for your contribution of {transaction.Merits}! The new bank balance is {newBalance.Item2} Merits");
                                }
                                MemberController.UpdateExperiencePoints("merits", transaction);

                            }
                        }
                        break;
                    case "withdraw":
                        if (bankers.Contains(ctx.Member.Id))
                        {
                            if (!ctx.Message.Content.ToLower().Contains("merit") && !ctx.Message.Content.ToLower().Contains("credit"))
                            {
                                var currency = await ctx.RespondAsync("Are you withdrawing Credits or Merits?");
                                var credEmojis = ConfirmEmojis(ctx, "credit");
                                await currency.CreateReactionAsync(credEmojis[0]);
                                await currency.CreateReactionAsync(credEmojis[1]);
                                Thread.Sleep(500);

                                var creditmsg = await interactivity.WaitForMessageReactionAsync(r => r == credEmojis[0] || r == credEmojis[1], currency, timeoutoverride: TimeSpan.FromMinutes(1));

                                try
                                {
                                    if (creditmsg.Emoji.Name == "💰")
                                        transaction = await BankController.GetBankActionAsync(ctx);

                                    else if (creditmsg.Emoji.Name == "🎖")
                                    {
                                        transaction = await BankController.GetBankActionAsync(ctx, false);
                                        isCredit = false;
                                    }
                                }
                                catch (Exception e)
                                {
                                    await ctx.RespondAsync("Please confirm Credits or Merits by clicking the appropriate reaction");
                                    break;
                                }
                            }
                            else if (ctx.Message.Content.ToLower().Contains("credit"))
                            {
                                transaction = await BankController.GetBankActionAsync(ctx);
                            }
                            else if (ctx.Message.Content.ToLower().Contains("merit"))
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
                                await ctx.RespondAsync($"You have successfully withdrawn {transaction.Merits}! The new bank balance is {newBalance.Item2} Merits");
                            }
                        }
                        else
                        {
                            await ctx.RespondAsync($"Only Bankers can make a withdrawal");
                        }
                        break;
                    case "balance":
                        var balanceembed = BankController.GetBankBalanceEmbed(ctx.Guild);
                        await ctx.RespondAsync(embed: balanceembed);
                        break;
                    case "exchange":
                        if (bankers.Contains(ctx.Member.Id))
                        {
                            var exchange = await ctx.RespondAsync("Are you Buying :regional_indicator_b: or Selling Merits?");
                            var credEmojis = ConfirmEmojis(ctx, "exchange");
                            await exchange.CreateReactionAsync(credEmojis[0]);
                            await exchange.CreateReactionAsync(credEmojis[1]);

                            var exMsg = await interactivity.WaitForMessageReactionAsync(r => r == credEmojis[0] || r == credEmojis[1], exchange, timeoutoverride: TimeSpan.FromMinutes(1));
                            if (exMsg.Emoji.Name == "🇧")
                            {
                                var buy = await ctx.RespondAsync("How many Merits are you buying?");
                                var merits = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1)));
                                var sell = await ctx.RespondAsync("What is the total amount you are spending to buy them?");
                                var credits = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1)));

                                var margin = BankController.ExchangeTransaction(ctx, "buy", int.Parse(credits.Message.Content), int.Parse(merits.Message.Content));

                                if(margin <= Convert.ToDecimal(1.5))
                                {
                                    await ctx.RespondAsync($"You bought {FormatHelpers.FormattedNumber(merits.Message.Content)} merits for {FormatHelpers.FormattedNumber(credits.Message.Content)} aUEC at a margin of {margin} that's a great deal!");
                                }
                                else
                                {
                                    await ctx.RespondAsync($"You bought {FormatHelpers.FormattedNumber(merits.Message.Content)} merits for {FormatHelpers.FormattedNumber(credits.Message.Content)} aUEC at a margin of {margin} please try to buy below 1.5");
                                }

                                buy.DeleteAsync();
                                sell.DeleteAsync(); 
                                merits.Message.DeleteAsync(); 
                                credits.Message.DeleteAsync();
                            }
                            else if (exMsg.Emoji.Name == "🇸")
                            {
                                var sell = await ctx.RespondAsync("How many Merits are you selling?");
                                var merits = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1)));
                                var buy = await ctx.RespondAsync("What is the total amount you are receiving?");
                                var credits = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1)));

                                var margin = BankController.ExchangeTransaction(ctx, "sell", int.Parse(credits.Message.Content), int.Parse(merits.Message.Content));
                                if (margin >= Convert.ToDecimal(2.5))
                                {
                                    await ctx.RespondAsync($"You sold {FormatHelpers.FormattedNumber(merits.Message.Content)} merits for {FormatHelpers.FormattedNumber(credits.Message.Content)} aUEC at a margin of {margin} that's a great deal!");
                                }
                                else
                                {
                                    await ctx.RespondAsync($"You sold {FormatHelpers.FormattedNumber(merits.Message.Content)} merits for {FormatHelpers.FormattedNumber(credits.Message.Content)} aUEC at a margin of {margin}, please try to sell greater thanw 2.5");
                                }

                                buy.DeleteAsync();
                                sell.DeleteAsync();
                                merits.Message.DeleteAsync();
                                credits.Message.DeleteAsync();
                            }

                        }
                        break;
                }

            }

            catch (Exception e)
            {
                await ctx.RespondAsync($"Unfortunately an error occured: {e}");
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
            BankController bankController = new BankController();

            var interactivity = ctx.Client.GetInteractivityModule();
            await ctx.RespondAsync("Are you sure you want to continue? This Cannot be undone");
            var confirmMsg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1));
     
            if(confirmMsg.Message.Content.ToLower() == "yes")
            {
                bankController.WipeBank(ctx.Guild);
                TransactionController.WipeTransactions(ctx.Guild);
                LoanController.WipeLoans(ctx);

                await ctx.RespondAsync("your org balance and transactions have been set to 0. All Loans have been completed");
            }
        }

        [Command("getreactions")]
        public async Task GetId(CommandContext ctx)
        {
            var interactivity = ctx.Client.GetInteractivityModule();
            var test =await  ctx.RespondAsync("you pick one! :poop: :100:");
            await test.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":poop:"));
            await test.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":100:"));

            List<DiscordEmoji> emojis = new List<DiscordEmoji>
            {
                DiscordEmoji.FromName(ctx.Client, ":poop:"),
                DiscordEmoji.FromName(ctx.Client, ":100:")
            };

            Thread.Sleep(500);
            var test2 = await interactivity.WaitForMessageReactionAsync(i => i == emojis[0] || i == emojis [1],test, timeoutoverride: TimeSpan.FromSeconds(5));

            try
            {
                if (test2.Emoji.Name == "💩")
                {
                    await test.RespondAsync("You know what screw you too buddy");
                }
                else if (test2.Emoji.Name == "💯")
                {
                    await test.RespondAsync("You're the dopest there is");
                }
                else
                {
                    await test.RespondAsync("wtf are you even doing here");
                }
            } catch(Exception e)
            {
                //await test.DeleteAsync();
                await ctx.RespondAsync("yah took to dang long you're the one who is 💩" );
            }
           

        }

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
            BankController bankController = new BankController();

            await ctx.RespondAsync("What is the ID of the ship you would like to fun");
            var interactivity = ctx.Client.GetInteractivityModule();
            var item = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1))).Message.Content;
            await ctx.RespondAsync("How many credits you put towards the ship");
            var credits = int.Parse((await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1))).Message.Content);

            await ctx.RespondAsync("Waiting for Banker to confirm the transfer");
            var bankers = await GetMembersWithRolesAsync("Banker", ctx.Guild);
            var confirmMsg = await interactivity.WaitForMessageAsync(xm => bankers.Contains(xm.Author.Id), TimeSpan.FromMinutes(10));
            if (confirmMsg.Message.Content.ToLower().Contains("yes")
                || confirmMsg.Message.Content.ToLower().Contains("confirm")
                || confirmMsg.Message.Content.ToLower().Contains("approve"))
            {
                BankTransaction trans = new BankTransaction("deposit", ctx.Member, ctx.Guild, credits);
                bankController.Deposit(trans);
                bankController.UpdateTransaction(trans);
                var xp = MemberController.UpdateExperiencePoints("credits for ships" ,trans);
                FleetController.UpdateFleetItemAmount(int.Parse(item), credits);
                await ctx.RespondAsync($"Your funds have been accepted and you've been credited the transaction.\n Your org experience is now {FormatHelpers.FormattedNumber(xp.ToString())}");
            }

        }

        private async Task LoanFund(CommandContext ctx)
        {
            var bankers = await GetMembersWithRolesAsync("Banker", ctx.Guild);

            await ctx.RespondAsync("Which Loan would you like to fund", embed: LoanController.GetWaitingLoansEmbed(ctx.Guild));

            var interactivity = ctx.Client.GetInteractivityModule();
            var loanIdMsg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1));

            Loans loan = null;

            if (bankers.Contains(ctx.Member.Id) && ctx.Message.Content.Contains("bank"))
            {
                var confirmEmojis = ConfirmEmojis(ctx);

                var approval = await ctx.RespondAsync("Are you sure you want to fund the loan with Bank funds?");
                await approval.CreateReactionAsync(confirmEmojis[0]);
                await approval.CreateReactionAsync(confirmEmojis[1]);
                Thread.Sleep(500);
                var confirmMsg = await interactivity.WaitForMessageReactionAsync(r => r == confirmEmojis[0] || r == confirmEmojis[1], approval, timeoutoverride: TimeSpan.FromMinutes(1));

                if (confirmMsg.Emoji.Name == "✅" && bankers.Contains(confirmMsg.User.Id))
                {
                    loan = await LoanController.FundLoan(loanIdMsg);
                }
                else 
                {
                    await ctx.RespondAsync("Loan funding with bank credits has been cancelled");
                }

            }
            else
            {
                loan = await LoanController.FundLoan(loanIdMsg);
            }

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

        private async Task<DiscordEmbed> LoanView(CommandContext ctx)
        {
            var plsHold = await ctx.RespondAsync("Getting your Loan Information, please hold");
            var embed = LoanController.GetLoanEmbed(ctx.Guild);
            await ctx.RespondAsync(embed: LoanController.GetLoanEmbed(ctx.Guild));
            plsHold.DeleteAsync();
            return embed;
        }



        private async Task LoanPayment(CommandContext ctx)
        {
            Loans loan = null;
            var pullingMsg = await ctx.RespondAsync("Pulling up your loan info now.");
            try
            {
                int memberId = MemberController.GetMemberbyDcId(ctx.Member, ctx.Guild).UserId;
                var loans = LoanController.GetLoanByApplicantId(memberId);

                if (loans != null)
                {
                    var interactivity = ctx.Client.GetInteractivityModule();

                    if(loans.Count > 1)
                    {
                        var loanCntMgs = await ctx.RespondAsync($"You have {loans.Count} outstanding loans. What is the is the id of the loan which you'd like to make a payment?");
                        var viewEmbed = await LoanView(ctx);
                        var loanIdMsg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1));
                        loan = loans.Find(x => x.LoanId == int.Parse(loanIdMsg.Message.Content));

                        loanIdMsg.Message.DeleteAsync();
                    }

                    
                    var balmsg = await ctx.RespondAsync($"Your current balance is {loan.RemainingAmount} much of your loan would you like to repay");
                    var amountmsg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(1));

                    var fundedMember = await ctx.Guild.GetMemberAsync(ulong.Parse(MemberController.GetMemberById(loan.FunderId.GetValueOrDefault()).DiscordId));
                    var confirmMsg = await ctx.RespondAsync($"{fundedMember.Mention} Please confirm this payment, you have 10 minutes to confirm");
                    var confirm = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == fundedMember.Id, TimeSpan.FromMinutes(10)));
                    if (confirm.Message.Content.Contains("yes") || confirm.Message.Content.Contains("confirm"))
                    {
                        LoanController.MakePayment(loan.LoanId, int.Parse(amountmsg.Message.Content));
                        await ctx.RespondAsync($"Payment of {FormatHelpers.FormattedNumber(amountmsg.Message.Content)} has been confirmed for loan - {loan.LoanId}: The new balance is {LoanController.GetLoanById(loan.LoanId).RemainingAmount}");
                    }

                    pullingMsg.DeleteAsync();
                    balmsg.DeleteAsync();
                    amountmsg.Message.DeleteAsync();
                    confirmMsg.DeleteAsync();
                    confirm.Message.DeleteAsync();
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

        private async Task<List<ulong>> GetMembersWithRolesAsync(string roleLevel, DiscordGuild guild)
        {
            var bankerRole = guild.Roles.FirstOrDefault(x => x.Name == roleLevel);
            var members = await guild.GetAllMembersAsync();
            List<ulong> bankersIds = new List<ulong>();
            foreach(var member in members)
            {
                if (member.Roles.Contains(bankerRole))
                {
                    bankersIds.Add(member.Id);
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

        private List<DiscordEmoji> ConfirmEmojis(CommandContext ctx, string group = "confirm")
        {
            List<DiscordEmoji> emojis = new List<DiscordEmoji>();

            if(group == "confirm")
            {
                emojis.Add(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
                emojis.Add(DiscordEmoji.FromName(ctx.Client, ":x:"));
            }
            else if(group == "credit")
            {
                emojis.Add(DiscordEmoji.FromName(ctx.Client, ":moneybag:"));
                emojis.Add(DiscordEmoji.FromName(ctx.Client, ":military_medal:"));
            }
            else if(group == "exchange")
            {
                emojis.Add(DiscordEmoji.FromName(ctx.Client, ":regional_indicator_b:"));
                emojis.Add(DiscordEmoji.FromName(ctx.Client, ":regional_indicator_s:"));
            }

            return emojis;
        }
    }
}