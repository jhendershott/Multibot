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
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;
using multicorp_bot.Controllers;
using multicorp_bot.Helpers;
using multicorp_bot.POCO;

namespace multicorp_bot
{
    public class Commands: BaseCommandModule
    {

        readonly Ranks Ranks;
        
        readonly MemberController MemberController;
        readonly TransactionController TransactionController;
        readonly LoanController LoanController;
        readonly FleetController FleetController;
        readonly OrgController OrgController;
        readonly WorkOrderController WorkOrderController;
        readonly DispatchController DispatchController;

        //Dan: I added variables here cause i didnt know how else to have them persist between commands and calls and stuff, idk if theres a better way
        //Other code added is CreateBoard(), UpdateBoard(), !view and then some changes to AcceptDispatch and GetWorkOrders so I could accomodate specific fishing of the iD when asked with !view.
        //
        DiscordMessage JobBoardMesage; 
        DiscordChannel Mychannel;
        //

        TelemetryHelper tHelper = new TelemetryHelper();

        public Commands()
        {
            Ranks = new Ranks();
            MemberController = new MemberController();
            TransactionController = new TransactionController();
            LoanController = new LoanController();
            FleetController = new FleetController();
            OrgController = new OrgController();
            WorkOrderController = new WorkOrderController();
            DispatchController = new DispatchController();
            PermissionsHelper.LoadPermissions();
        }

        [Command("handle")]
        public async Task UpdateHandle(CommandContext ctx, params string [] handle)
        {
             TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "handle", ctx);
            DiscordMember member = null;
            try
            {
                string handlestr = string.Join(" ", handle);
                string newNick = null;

                member = ctx.Member;
                 newNick = Ranks.GetUpdatedNickname(member, handlestr);

                MemberController.UpdateMemberName(ctx, Ranks.GetNickWithoutRank(member), Ranks.GetNickWithoutRank(newNick), ctx.Guild);
                await member.ModifyAsync(x => x.Nickname = newNick);
            }
            catch (Exception e)
            {
                tHelper.LogException($"Method: UpdateHandle; Org: {ctx.Guild.Name}; Message: {ctx.Message}; User:{ctx.Member.Nickname}", e);
                Console.WriteLine(e.Message);
            }
        }

        [Command("multibot-help")]
        public async Task Help(CommandContext ctx, string helpCommand = null)
        {
            TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "multibot-help", ctx);

            if (helpCommand == null) { 

                await ctx.RespondAsync("Which command would you like help with? Bank, Loans, Handle, Promotion, Fleet, Dispatch or Log or Wipe?");
                var interactivity = ctx.Client.GetInteractivity();
                var optMessage = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5));
                helpCommand = optMessage.Result.Content;
            }

            switch (helpCommand.ToLower())
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
                case "dispatch":
                    await ctx.RespondAsync(embed: HelpController.DispatchEmbed());
                    break;
                case "log":
                    await ctx.RespondAsync(embed: HelpController.LogEmbed());
                    break;
            }
        }

        [Command("setup")]
        public async Task Setup(CommandContext ctx)
        {
            OrgController.AddOrg(ctx.Guild);
            await ctx.RespondAsync("You're all setup and ready to go");
        }


        [Command("check-requirements")]
        public async Task CheckRequirements(CommandContext ctx)
        {
            TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "check-requirements", ctx);

            try
            {
                string missingRequirements = "";

                foreach (var item in Ranks.MilRanks)
                {
                    if (!ctx.Guild.Roles.Select(x => x.Value.Name).Contains(item.RankName))
                        missingRequirements += $"Rank {item.RankName} missing\n";
                }

                await ctx.RespondAsync(missingRequirements);
            }
            catch (Exception e)
            {
                TelemetryHelper.Singleton.LogException("check-requirements", e);
                Console.WriteLine(e.Message);
            }

        }

        [Command("check")]
        public async Task Check(CommandContext ctx, DiscordUser user)
        {
            TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "check", ctx);
            try
            {
                var level = PermissionsHelper.GetPermissionLevel(ctx.Guild, user);
                Console.WriteLine(level);
                await ctx.RespondAsync($"The permission level of {user.Mention} is: {level}");
            }
            catch (Exception e)
            {
                TelemetryHelper.Singleton.LogException("check", e);
                Console.WriteLine(e.Message);
            }
        }

        [Command("set-role-level")]
        public async Task SetRoleLevel(CommandContext ctx, DiscordRole role, int level)
        {
            if (PermissionsHelper.GetPermissionLevel(ctx.Guild, ctx.User) < 2)
            {
                TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "set-role-level-denied", ctx);
                return;
            }
               
            TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "set-role-level", ctx);

            try
            {
                PermissionsHelper.SetRolePermissionLevel(role, level);
                await ctx.RespondAsync($"{role.Mention} is now assigned to level {level}");
            }
            catch (Exception e)
            {
                TelemetryHelper.Singleton.LogException("set-role-level", e);
                Console.WriteLine(e.Message);
            }
        }

        [Command("promote")]
        public async Task PromoteMember(CommandContext ctx, params DiscordMember[] members)
        {
            try
            {
                if (!PermissionsHelper.CheckPermissions(ctx, Permissions.ManageRoles) && !PermissionsHelper.CheckPermissions(ctx, Permissions.ManageNicknames))
                {
                    TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "promote-denied", ctx);
                    await ctx.RespondAsync("You can't do that you don't have the power!");
                    return;
                }

                TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "promote", ctx);

                string congrats = $"Congratulations on your promotion :partying_face:";

                foreach (var user in members)
                {

                    DiscordMember member = await ctx.Guild.GetMemberAsync(user.Id);
                    await Ranks.Promote(member);
                    await member.ModifyAsync(x => x.Nickname = Ranks.GetUpdatedNickname(member));
                    congrats = congrats += $" {member.Mention}";
                    TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "promote-congrats", ctx, member);
                }

                await ctx.Message.DeleteAsync();
                await ctx.RespondAsync(congrats);
            }
            catch (Exception e)
            {
                tHelper.LogException($"Promote error {ctx.Guild.Name}; Message: {ctx.Message}; User:{ctx.Member.Nickname}", e);
                Console.WriteLine(e);
            }
        }

        [Command("demote")]
        public async Task DemoteMember(CommandContext ctx, params DiscordMember[] members)
        {
            if (!PermissionsHelper.CheckPermissions(ctx, Permissions.ManageRoles) && !PermissionsHelper.CheckPermissions(ctx, Permissions.ManageNicknames))
            {
                TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "demote-denied", ctx);
                await ctx.RespondAsync("You can't do that you don't have the power!");
                return;
            }

            TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "demote", ctx);

            string congrats = $"Oh no you've been demoted! What have you done :disappointed_relieved:";
            foreach (var user in members)
            {
                DiscordMember member = await ctx.Guild.GetMemberAsync(user.Id);
                await Ranks.Demote(member);

                await member.ModifyAsync(x => x.Nickname = Ranks.GetUpdatedNickname(member, -1));
                congrats = congrats += $" {member.Mention}";
                TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "demote-congrats", ctx, member);
            }
            await ctx.Message.DeleteAsync();
            await ctx.RespondAsync(congrats);
        }

        [Command("recruit")]
        public async Task RecruitMember(CommandContext ctx, DiscordMember member)
        {
            if (PermissionsHelper.GetPermissionLevel(ctx.Guild, ctx.User) < 1)
            {
                TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "recruit-denied", ctx);
                return;
            }

            TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "recruit", ctx);

            await Ranks.Recruit(member);
            await ctx.RespondAsync($"Welcome on board {member.Mention} :alien:");
        }

        [Command("bank")]
        public async Task Bank(CommandContext ctx, string command)
        {
            TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "bank", ctx);

            BankController BankController = new BankController();
            string[] args = Regex.Split(ctx.Message.Content, @"\s+");
            Tuple<string, string> newBalance;
            var interactivity = ctx.Client.GetInteractivity();
            BankTransaction transaction = null;
            var bankers = await GetMembersWithRolesAsync("Banker", ctx.Guild);
            bool isCredit = true;

            try
            {
                switch (command.ToLower())
                {
                    case "balance":
                        try
                        {
                            Console.Write($"Balance Command accepts for org {ctx.Guild.Name}");
                            var balanceembed = BankController.GetBankBalanceEmbed(ctx.Guild);
                            Console.WriteLine("able to get embed");
                            await ctx.RespondAsync(embed: balanceembed);
                            Console.WriteLine("response attemped");
                            break;
                        }
                        catch (Exception e)
                        {
                            tHelper.LogException($"Method: Bank Balance; Org: {ctx.Guild.Name}; Message: {ctx.Message}; User:{ctx.Member.Nickname}", e);
                            await ctx.RespondAsync($"I feel cold wnr - {e.Message}");
                            break;
                        }
                    case "reconcile":
                        try
                        {
                            if (bankers.Contains(ctx.Member.Id))
                            {
                                await ctx.RespondAsync("How many merits are in the bank?");
                                var merits = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5)));
                                await ctx.RespondAsync("How many credits are in the bank?");
                                var credits = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5)));

                                var differences = BankController.Reconcile(ctx, merits.Result.Content, credits.Result.Content);
                                await ctx.RespondAsync($"Unaccounted for differences: \n {differences.Item1} credits, \n {differences.Item2} merits");
                            }
                            break;
                        }
                        catch (Exception e)
                        {
                            tHelper.LogException($"Method: Bank Reconcile; Org: {ctx.Guild.Name}; Message: {ctx.Message}; User:{ctx.Member.Nickname}", e);
                            break;
                        }
                    case "exchange":
                        try
                        {
                            if (bankers.Contains(ctx.Member.Id))
                            {
                                var exchange = await ctx.RespondAsync("Are you Buying :regional_indicator_b: or Selling Merits?");
                                var credEmojis = ConfirmEmojis(ctx, "exchange");
                                await exchange.CreateReactionAsync(credEmojis[0]);
                                await exchange.CreateReactionAsync(credEmojis[1]);
                                Thread.Sleep(500);
                                    
                                var exMsg = await interactivity.WaitForReactionAsync(r => r.Emoji == credEmojis[0] || r.Emoji == credEmojis[1], timeoutoverride: TimeSpan.FromMinutes(5));
                                if (exMsg.Result.Emoji.Name == "🇧")
                                {
                                    var buy = await ctx.RespondAsync("How many Merits are you buying?");
                                    var merits = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5)));
                                    var sell = await ctx.RespondAsync("What is the total amount you are spending to buy them?");
                                    var credits = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5)));

                                    var margin = BankController.ExchangeTransaction(ctx, "buy", int.Parse(credits.Result.Content), int.Parse(merits.Result.Content));

                                    if (margin <= Convert.ToDecimal(1.5))
                                    {
                                        await ctx.RespondAsync($"You bought {FormatHelpers.FormattedNumber(merits.Result.Content)} merits for {FormatHelpers.FormattedNumber(credits.Result.Content)} aUEC at a margin of {margin} that's a great deal!");
                                    }
                                    else
                                    {
                                        await ctx.RespondAsync($"You bought {FormatHelpers.FormattedNumber(merits.Result.Content)} merits for {FormatHelpers.FormattedNumber(credits.Result.Content)} aUEC at a margin of {margin} please try to buy below 1.5");
                                    }

                                    buy.DeleteAsync();
                                    sell.DeleteAsync();
                                    merits.Result.DeleteAsync();
                                    credits.Result.DeleteAsync();
                                }
                                else if (exMsg.Result.Emoji.Name == "🇸")
                                {
                                    var sell = await ctx.RespondAsync("How many Merits are you selling?");
                                    var merits = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5)));
                                    var buy = await ctx.RespondAsync("What is the total amount you are receiving?");
                                    var credits = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5)));

                                    var margin = BankController.ExchangeTransaction(ctx, "sell", int.Parse(credits.Result.Content), int.Parse(merits.Result.Content));
                                    if (margin >= Convert.ToDecimal(2.5))
                                    {
                                        await ctx.RespondAsync($"You sold {FormatHelpers.FormattedNumber(merits.Result.Content)} merits for {FormatHelpers.FormattedNumber(credits.Result.Content)} aUEC at a margin of {margin} that's a great deal!");
                                    }
                                    else
                                    {
                                        await ctx.RespondAsync($"You sold {FormatHelpers.FormattedNumber(merits.Result.Content)} merits for {FormatHelpers.FormattedNumber(credits.Result.Content)} aUEC at a margin of {margin}, please try to sell greater than 2.5");
                                    }

                                    buy.DeleteAsync();
                                    sell.DeleteAsync();
                                    merits.Result.DeleteAsync();
                                    credits.Result.DeleteAsync();
                                }

                            }
                            break;
                        }
                        catch (Exception e)
                        {
                            tHelper.LogException($"Method: Bank Exchange; Org: {ctx.Guild.Name}; Message: {ctx.Message}; User:{ctx.Member.Nickname}", e);
                            break;
                        }
                }
            }
            catch (Exception e)
            {
                tHelper.LogException($"Method: Bank Exchange; Org: {ctx.Guild.Name}; Message: {ctx.Message}; User:{ctx.Member.Nickname}", e);
                await ctx.RespondAsync($"Something went heinously wrong {e.Message}");
            }
        }

        [Command("bank")]
        public async Task Bank(CommandContext ctx, string command, DiscordMember user, int amount, string type = null)
        {
            BankController BankController = new BankController();
            Tuple<string, string> newBalance;
            var interactivity = ctx.Client.GetInteractivity();
            BankTransaction transaction = null;
            var bankers = await GetMembersWithRolesAsync("Banker", ctx.Guild);
            bool isCredit = true;

            try
            {
                switch (command.ToLower())
                {
                    case "deposit":
                        TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "bank-deposit", ctx);
                        if (!bankers.Contains(ctx.Member.Id))
                        {
                            await ctx.RespondAsync("I'm sorry only a Banker can deposit on another members behalf");
                        }
                        else
                        {
                            if (type == null)
                            {
                                await ctx.RespondAsync("Starting your deposit, Would you like to deposit credits or merits?");

                                var confirmMsg = await interactivity.WaitForMessageAsync(r => r.Author == ctx.User && r.Content.ToLower().Contains("merit") && r.Content.ToLower().Contains("credit"), timeoutoverride: TimeSpan.FromMinutes(20));

                                if (!confirmMsg.Result.Content.ToLower().Contains("merit") && !confirmMsg.Result.Content.ToLower().Contains("credit"))
                                {
                                    await ctx.RespondAsync("Please specify credits or merits e.g. !bank deposit 1000 merits || !bank deposit 1000 credits");
                                }
                                else
                                {
                                    type = confirmMsg.Result.Content.ToLower();
                                }

                            }


                            if (type.Contains("credit"))
                            {
                                TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "bank-deposit-credit", ctx);
                                transaction = await BankController.GetBankActionAsync(ctx, "deposit", amount, user, "credit");
                            }
                            else if (type.Contains("merit"))
                            {
                                TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "bank-deposit-merit", ctx);
                                transaction = await BankController.GetBankActionAsync(ctx, "deposit", amount, user, "merit");
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

                }

            }

            catch (Exception e)
            {
                tHelper.LogException($"Method: Bank Uncaught exception; Org: {ctx.Guild.Name}; Message: {ctx.Message}; User:{ctx.Member.Nickname}", e);
                Console.WriteLine(e.Message);
                await ctx.Channel.SendMessageAsync($"Send WNR the following error: {e}");
            }
        }

        [Command("bank")]
        public async Task Bank(CommandContext ctx, string command, int amount)
        {
            TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "bank", ctx);

            BankController BankController = new BankController();
            string[] args = Regex.Split(ctx.Message.Content, @"\s+");
            Tuple<string, string> newBalance;
            var interactivity = ctx.Client.GetInteractivity();
            BankTransaction transaction = null;
            var bankers = await GetMembersWithRolesAsync("Banker", ctx.Guild);
            bool isCredit = true;

            try
            {
                switch (command.ToLower())
                {
                    case "deposit":
                        TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "bank-deposit", ctx);
                        if (!bankers.Contains(ctx.Member.Id))
                        {
                            await ctx.RespondAsync("Starting your deposit, Would you like to deposit credits or merits?");

                            var confirmMsg = await interactivity.WaitForMessageAsync(r => r.Author == ctx.User && r.Content.ToLower().Contains("merit") && r.Content.ToLower().Contains("credit"), timeoutoverride: TimeSpan.FromMinutes(20));

                            if (!confirmMsg.Result.Content.ToLower().Contains("merit") && !confirmMsg.Result.Content.ToLower().Contains("credit"))
                            {
                                await ctx.RespondAsync("Please specify credits or merits e.g. !bank deposit 1000 merits || !bank deposit 1000 credits");
                            }


                            if (confirmMsg.Result.Content.ToLower().Contains("credit"))
                            {
                                TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "bank-deposit-credit", ctx);
                                transaction = await BankController.GetBankActionAsync(ctx, "deposit", amount, null, "credit");
                            }
                            else if (confirmMsg.Result.Content.ToLower().Contains("merit"))
                            {
                                TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "bank-deposit-merit", ctx);
                                transaction = await BankController.GetBankActionAsync(ctx, "deposit", amount, null, "merit");
                                isCredit = false;
                            }


                            var approval = await ctx.RespondAsync("Banker please confirmed this request by replying with 'approve', 'yes' or 'confirm'");
                            var bankConfirmMsg = await interactivity.WaitForMessageAsync(r => bankers.Contains(r.Author.Id), timeoutoverride: TimeSpan.FromMinutes(20));
                            try
                            {
                                var confirmText = confirmMsg.Result.Content.ToLower();
                                if (confirmText.Contains("yes") || confirmText.Contains("confirm") || confirmText.Contains("approve"))
                                {
                                    TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "bank-deposit-action-confirm", ctx);

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
                                else if (!bankers.Contains(confirmMsg.Result.Author.Id))
                                {
                                    TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "bank-deposit-unauth-approval", ctx);
                                    await ctx.RespondAsync("Looks like someone who isn't a banker attempted to approve the transactions. " +
                                        "Only bankers can approve transactions");
                                }

                            }
                            catch (Exception e)
                            {
                                tHelper.LogException($"Method: Bank Deposit; Org: {ctx.Guild.Name}; Message: {ctx.Message}; User:{ctx.Member.Nickname}", e);
                                await ctx.RespondAsync("Either there was no confirmation or there was an error, please try again when a Banker is available to assist you");
                                break;
                            }
                        }
                        else
                        {
                            if (!ctx.Message.Content.ToLower().Contains("merit") && !ctx.Message.Content.ToLower().Contains("credit"))
                            {
                                await ctx.RespondAsync("Starting your deposit, Would you like to deposit credits or merits?");

                                var confirmMsg = await interactivity.WaitForMessageAsync(r => r.Author == ctx.User && (r.Content.ToLower().Contains("merit") || r.Content.ToLower().Contains("credit")), timeoutoverride: TimeSpan.FromMinutes(20));

                                if (!confirmMsg.Result.Content.ToLower().Contains("merit") && !confirmMsg.Result.Content.ToLower().Contains("credit"))
                                {
                                    await ctx.RespondAsync("Please specify credits or merits e.g. !bank deposit 1000 merits || !bank deposit 1000 credits");
                                }


                                if (confirmMsg.Result.Content.ToLower().Contains("credit"))
                                {
                                    TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "bank-deposit-credit", ctx);
                                    transaction = await BankController.GetBankActionAsync(ctx, "deposit", amount, null, "credit");
                                }
                                else if (confirmMsg.Result.Content.ToLower().Contains("merit"))
                                {
                                    TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "bank-deposit-merit", ctx);
                                    transaction = await BankController.GetBankActionAsync(ctx, "deposit", amount,null, "merit");
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
                        }
                        break;
                        
                    case "withdraw":
                        try
                        {
                            await ctx.RespondAsync("starting Withdraw process");
                            if (bankers.Contains(ctx.Member.Id))
                            {
                                if (!ctx.Message.Content.ToLower().Contains("merit") && !ctx.Message.Content.ToLower().Contains("credit"))
                                {
                                    var currency = await ctx.RespondAsync("Are you withdrawing Credits or Merits? please respond with 'credits' or 'merits'");

                                    Thread.Sleep(1000);

                                    var creditmsg = await interactivity.WaitForMessageAsync(r => (r.Content.ToLower().Contains("credit") || r.Content.ToLower().Contains("merit")) && !r.Author.Username.ToLower().Contains("multibot"));

                                    try
                                    {
                                        if (creditmsg.Result.Content.ToLower().Contains("credit"))
                                        {
                                            transaction = await BankController.GetBankActionAsync(ctx, "withdraw", amount, type: "credit");
                                        }

                                        else if (creditmsg.Result.Content.ToLower().Contains("merit"))
                                        {
                                            transaction = await BankController.GetBankActionAsync(ctx, "withdraw", amount, type: "merit");
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
                                    transaction = await BankController.GetBankActionAsync(ctx, "withdraw", amount, type: "credit");
                                }
                                else if (ctx.Message.Content.ToLower().Contains("merit"))
                                {
                                    transaction = await BankController.GetBankActionAsync(ctx, "withdraw", amount, type: "merit");
                                    isCredit = false;
                                }
                                newBalance = BankController.Withdraw(transaction);
                                if (isCredit)
                                {
                                    await ctx.RespondAsync($"You have successfully withdrawn {transaction.Amount} aUEC! The new bank balance is {newBalance.Item1} aUEC");
                                }
                                else
                                {
                                    await ctx.RespondAsync($"You have successfully withdrawn {transaction.Merits} Merits! The new bank balance is {newBalance.Item2} Merits");
                                }
                            }
                            else
                            {
                                await ctx.RespondAsync($"Only Bankers can make a withdrawal");
                            }
                            break;
                        }
                        catch (Exception e)
                        {
                            tHelper.LogException($"Method: Bank WithDraw; Org: {ctx.Guild.Name}; Message: {ctx.Message}; User:{ctx.Member.Nickname}", e);
                            break;
                        }
                }

            }

            catch (Exception e)
            {
                tHelper.LogException($"Method: Bank Uncaught exception; Org: {ctx.Guild.Name}; Message: {ctx.Message}; User:{ctx.Member.Nickname}", e);
                Console.WriteLine(e.Message);
            }
        }

        [Command("bank")]
        public async Task Bank(CommandContext ctx, string command, int amount, string type)
        {
            TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "bank", ctx);

            BankController BankController = new BankController();
            string[] args = Regex.Split(ctx.Message.Content, @"\s+");
            Tuple<string, string> newBalance;
            var interactivity = ctx.Client.GetInteractivity();
            BankTransaction transaction = null;
            var bankers = await GetMembersWithRolesAsync("Banker", ctx.Guild);
            bool isCredit = true;

            try
            {
                switch (command.ToLower())
                {
                    case "deposit":
                        TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "bank-deposit", ctx);
                        if (!bankers.Contains(ctx.Member.Id))
                        {
                            var confirm = await ctx.RespondAsync("Starting your deposit, please be aware if a banker is not present the transaction will timeout");

                            if (!type.ToLower().Contains("merit") && !type.ToLower().Contains("credit"))
                            {
                                await ctx.RespondAsync("Please specify credits or merits e.g. !bank deposit 1000 merits || !bank deposit 1000 credits");
                            }
                                

                            if (type.ToLower().Contains("credit"))
                            {
                                TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "bank-deposit-credit", ctx);
                                transaction = await BankController.GetBankActionAsync(ctx, "deposit", amount, type: type);
                            }
                            else if(type.ToLower().Contains("merit"))
                            {
                                TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "bank-deposit-merit", ctx);
                                transaction = await BankController.GetBankActionAsync(ctx, "deposit", amount, type: type);
                                isCredit = false;
                            }

                            var approval = await ctx.RespondAsync("Banker please confirmed this request by replying with 'approve', 'yes' or 'confirm'");
                            var confirmMsg = await interactivity.WaitForMessageAsync(r => bankers.Contains(r.Author.Id), timeoutoverride: TimeSpan.FromMinutes(20));
                            try
                            {
                                var confirmText = confirmMsg.Result.Content.ToLower();
                                if (confirmText.Contains("yes") || confirmText.Contains("confirm") || confirmText.Contains("approve"))
                                {
                                    TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "bank-deposit-action-confirm", ctx);

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
                                else if (!bankers.Contains(confirmMsg.Result.Author.Id))
                                {
                                    TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "bank-deposit-unauth-approval", ctx);
                                    await ctx.RespondAsync("Looks like someone who isn't a banker attempted to approve the transactions. " +
                                        "Only bankers can approve transactions");
                                }

                                await confirm.DeleteAsync();
                            }
                            catch (Exception e)
                            {
                                tHelper.LogException($"Method: Bank Deposit; Org: {ctx.Guild.Name}; Message: {ctx.Message}; User:{ctx.Member.Nickname}", e);
                                await ctx.RespondAsync("Either there was no confirmation or there was an error, please try again when a Banker is available to assist you");
                                break;
                            }
                        }
                        else
                        {
                            if (!type.ToLower().Contains("merit") && !type.ToLower().Contains("credit"))
                            {
                                await ctx.RespondAsync("Please specify credits or merits e.g. !bank deposit 1000 merits || !bank deposit 1000 credits");
                            }


                            if (type.ToLower().Contains("credit"))
                            {
                                TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "bank-deposit-credit", ctx);
                                transaction = await BankController.GetBankActionAsync(ctx, "deposit", amount, type: type);
                            }
                            else if (type.ToLower().Contains("merit"))
                            {
                                TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "bank-deposit-merit", ctx);
                                transaction = await BankController.GetBankActionAsync(ctx, "deposit", amount, type: type);
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
                        try
                        {
                            await ctx.RespondAsync("starting Withdraw process");
                            if (bankers.Contains(ctx.Member.Id))
                            {
                                if (!type.ToLower().Contains("merit") && !type.ToLower().Contains("credit"))
                                {
                                    await ctx.RespondAsync("Please specify credits or merits e.g. !bank deposit 1000 merits || !bank deposit 1000 credits");
                                }

                                try
                                {
                                    if (type.ToLower().Contains("credit"))
                                    {
                                        transaction = await BankController.GetBankActionAsync(ctx, "withdraw", amount, type: type);
                                    }

                                    else if (type.ToLower().Contains("merit"))
                                    {
                                        transaction = await BankController.GetBankActionAsync(ctx, "withdraw", amount, type: type);
                                        isCredit = false;
                                    }
                                }
                                catch (Exception e)
                                {
                                    await ctx.RespondAsync("Please confirm Credits or Merits by clicking the appropriate reaction");
                                    break;
                                }




                                newBalance = BankController.Withdraw(transaction);
                                if (isCredit)
                                {
                                    await ctx.RespondAsync($"You have successfully withdrawn {transaction.Amount} aUEC! The new bank balance is {newBalance.Item1} aUEC");
                                }
                                else
                                {
                                    await ctx.RespondAsync($"You have successfully withdrawn {transaction.Merits} Merits! The new bank balance is {newBalance.Item2} Merits");
                                }
                            }
                            else
                            {
                                await ctx.RespondAsync($"Only Bankers can make a withdrawal");
                            }
                            break;
                        }
                        catch (Exception e)
                        {
                            tHelper.LogException($"Method: Bank WithDraw; Org: {ctx.Guild.Name}; Message: {ctx.Message}; User:{ctx.Member.Nickname}", e);
                            break;
                        }
                }

            }

            catch (Exception e)
            {
                tHelper.LogException($"Method: Bank Uncaught exception; Org: {ctx.Guild.Name}; Message: {ctx.Message}; User:{ctx.Member.Nickname}", e);
                Console.WriteLine(e.Message);
            }
        }

        [Command("fleet")]
        public async Task Fleet(CommandContext ctx, string arg)
        {
            TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "fleet", ctx);

            switch (arg.ToLower()){
                case "view":
                    TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "fleet-view", ctx);
                    await ctx.RespondAsync(embed: new FleetController().GetFleetRequests(ctx.Guild));
                    break;
                case "request":
                    TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "fleet-request", ctx);
                    await FleetRequest(ctx);
                    break;
                case "fund":
                    TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "fleet-fund", ctx);
                    await FundFleet(ctx);
                    break;
                case "complete":
                    TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "fleet-complete", ctx);
                    var completed = FleetController.CompleteFleetRequest(ctx.Guild);
                    await ctx.RespondAsync($"{completed} requests have been marked complete");
                    break;
            }
            
        }

        [Command("loan")]
        public async Task Loan(CommandContext ctx)
        {
            await ctx.RespondAsync($"Please try again: Options for loans is 'request', 'view', 'payment' (or 'pay', 'fund', and 'complete'\n For Example !loan request");
        }

        [Command("loan")]
        public async Task Loan(CommandContext ctx, string command)
        {
            TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "loan", ctx);
            switch (command.ToLower())
            {
                case "request":
                    TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "loan-request", ctx);
                    await LoanRequest(ctx);
                    break;
                case "view":
                    TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "loan-view", ctx);
                    await LoanView(ctx);
                    break;
                case "payment":
                    TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "loan-payment", ctx);
                    await LoanPayment(ctx);
                    break;
                case "pay":
                    TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "loan-pay", ctx);
                    await LoanPayment(ctx);
                    break;
                case "fund":
                    TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "loan-fund", ctx);
                    await LoanFund(ctx);
                    break;
                case "complete":
                    TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "loan-complete", ctx);
                    await LoanComplete(ctx);
                    break;
                //case "add": LoanController.AddLoan(ctx.Member, ctx.Guild, 50000, 1000);
                //    break;
                default:
                    TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "loan-options", ctx);
                    await ctx.RespondAsync("Options for loans is 'request', 'view', 'payment', 'fund', and 'complete'");
                    break;
            }
        }
        
        [Command("loan")]
        public async Task Loan(CommandContext ctx, string command, string qualifier)
        {
            TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "loan", ctx);
            switch (command.ToLower())
            {
                case "fund":
                    TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "loan-fund", ctx);
                    await LoanFund(ctx, qualifier);
                    break;
                case "complete":
                    TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "loan-complete", ctx);
                    await LoanComplete(ctx, qualifier);
                    break;
                //case "add": LoanController.AddLoan(ctx.Member, ctx.Guild, 50000, 1000);
                //    break;
                default:
                    TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "loan-options", ctx);
                    await ctx.RespondAsync("Options for loans is 'request', 'view', 'payment', 'fund', and 'complete'");
                    break;
            }
        }

        [Command("accept")]
        public async Task Dispatch(CommandContext ctx, int? id = null)
        {
            try
            {
                var interactivity = ctx.Client.GetInteractivity();

                if (id == null)
                {
                    await ctx.RespondAsync("What is the id you would like to accept?");
                    id = int.Parse((await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5))).Result.Content);
                }

                WorkOrderController controller = new WorkOrderController();
                await controller.AcceptWorkOrder(ctx, id.GetValueOrDefault());
            } catch (Exception e)
            {
                await ctx.RespondAsync("I'm sorry, I was unable to process your request due to an error");
            }

        }

        [Command("view")]
        public async Task view(CommandContext ctx, int? id = null)
        {

            try
            {
                if (id == null)
                { 
                await ctx.RespondAsync("Provide the ID after !view, please try again");
                }
                else
                {
                    var controller = new WorkOrderController();
                    var interactivity = ctx.Client.GetInteractivity();
                    var wOrder = await controller.GetWorkOrders(ctx, "Shipping", id);
                    var msg = await ctx.RespondAsync(embed: wOrder.Item1);

                }
            }
            catch (Exception e)
            {
                await ctx.RespondAsync("Yo WNR someone broke me, check the logs");
            }
        }

        [Command("dispatch")]
        public async Task Dispatch(CommandContext ctx, string type = null, int? id = null)
        {
            try
            {
                List<DiscordMessage> messages = new List<DiscordMessage>();
                TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "dispatch", ctx);

                var interactivity = ctx.Client.GetInteractivity();
                WorkOrderController controller = new WorkOrderController();
                if (type == null)
                {
                    messages.Add(await ctx.RespondAsync("Are you looking to 'Add' or 'Accept' a dispatch"));
                    type = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5))).Result.Content;
                    if (type.ToLower() != "add" && type.ToLower() != "accept")
                    {
                        messages.Add(await ctx.RespondAsync("Please start over and use either 'add' or 'accept' at the prompt or try '!dispatch add' or '!dispatch accept'"));
                    }                   
                }
                
                if (type.ToLower() == "accept")
                {
                    messages.Add(await ctx.RespondAsync("What type of work are you interested in Mining, Roc Mining, Hand Mining, Trading, Shipping, or Military?"));
                    type = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5))).Result.Content;
                    if (type.ToLower() != "add" && type.ToLower() != "accept")
                    {
                        var initialAccept = await AcceptDispatch(ctx, type);
                        if (initialAccept.Item1)
                        {
                            TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "dispatch-accepted", ctx);
                            id = initialAccept.Item2.Id;
                        }
                        else
                        {
                            messages.Add(await ctx.RespondAsync($"You can view up to 3 more work orders *NOTE* if there are less than 3 work orders you will get duplicates\n" +
                                    $"you can can accept a previous work order by sending !Dispatch Accept [previous work order id]\n" +
                                    $"or can cancel the dispatch by simple allowing it to time out (two minutes)"));
                            for (int i = 3; i > 0; i--)
                            {
                                var subsequent = await AcceptDispatch(ctx, type);
                                if (subsequent.Item1)
                                {
                                    TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "dispatch-accepted", ctx);
                                    id = subsequent.Item2.Id;
                                    break;
                                }

                                
                            }
                        }
                    }

                    if (id == null)
                    {
                        messages.Add(await ctx.RespondAsync("What is the ID of the work order would you like accept?"));
                        id = int.Parse((await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5))).Result.Content);
                    }
                    if (await controller.AcceptWorkOrder(ctx, id.GetValueOrDefault()))
                    {
                        TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "dispatch-accepted", ctx);
                        await ctx.RespondAsync("Work order has been accepted");
                    }
                    else
                    {
                        TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "dispatch-failed", ctx);
                        await ctx.RespondAsync("Something went wrong trying to accept the order");
                    }
                }
                else if (type.ToLower() == "add")
                {
                    TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "dispatch-added", ctx);
                    await AddWorkOrder(ctx);
                }
                else if (type.ToLower() == "view")

                {
                    TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "dispatch-view", ctx);
                    await ctx.Channel.SendMessageAsync(embed: await WorkOrderController.GetWorkOrderByMember(ctx));
                }
                else if (type.ToLower() == "log")
                {
                    await Log(ctx);
                }
                else
                {
                    var initialAccept = await AcceptDispatch(ctx, type);
                    if (initialAccept.Item1)
                    {
                        TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "dispatch-accepted", ctx);
                        await controller.AcceptWorkOrder(ctx, initialAccept.Item2.Id);
                    }
                    else
                    {
                        messages.Add(await ctx.RespondAsync($"You can view up to 3 more work orders *NOTE* if there are less than 3 work orders you will get duplicates\n" +
                                $"you can can accept a previous work order by sending !Dispatch Accept [previous work order id]\n" +
                                $"or can cancel the dispatch by simple allowing it to time out (two minutes)"));
                        for (int i = 3; i > 0; i--)
                        {
                            var subsequent = await AcceptDispatch(ctx, type);
                            if (subsequent.Item1)
                            {
                                await controller.AcceptWorkOrder(ctx, subsequent.Item2.Id);
                                break;
                            }
                        }
                    }
                }

                foreach (var mess in messages)
                {
                    await mess.DeleteAsync();
                }
            } catch(Exception e)
            {
                await ctx.RespondAsync("Yo WNR someone broke me, check the logs");
            }
        }

        [Command("updateBoard")]   //command to test updating the board, we should call UpdateJobBoard() everytime we remove and add orders.
        public async Task updateBoard(CommandContext ctx)
        {
            await UpdateJobBoard(ctx);

        }

        public async Task UpdateJobBoard(CommandContext ctx)
        {
            try
            {
                var Mychannel = (await ctx.Guild.GetChannelsAsync()).FirstOrDefault(x => x.Name == "job-board");
                if (Mychannel == null)
                {
                    await ctx.Channel.SendMessageAsync("For a cleaner and more readable experience you must create a channel called 'job-board'");
                }
                else
                {
                    if (JobBoardMesage != null)
                    {
                        await JobBoardMesage.DeleteAsync();
                    }

                    JobBoardMesage = await Mychannel.SendMessageAsync(embed: await WorkOrderController.CreateJobBoard(ctx, "Shipping"));
                }
            } catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [Command("restoreNicknames")]
        public async Task RestoreNicks(CommandContext ctx)
        {
            await MemberController.RestoreRanks(ctx);
        }

        [Command("log")]
        public async Task Log(CommandContext ctx, string? workOrder = null, string? requirementId = null, string? amount = null)
        {
            TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "log", ctx);

            WorkOrderController controller = new WorkOrderController();
            var interactivity = ctx.Client.GetInteractivity();
            string material;

            if (workOrder == null)
            {
                await ctx.RespondAsync("What order would you like log against?");
                workOrder = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5))).Result.Content;
            }

            if (requirementId == null)
            {
                await ctx.RespondAsync("What type or material would you like to log (the material name, not the id)");
                material = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5))).Result.Content;
            }
            else
            {
                material = controller.GetRequirementById(int.Parse(requirementId)).Material;
            }

            if (amount == null)
            {
                await ctx.RespondAsync("How much would you like to log?");
                var msg = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5))).Result.Content;
                amount = Regex.Replace(msg,  "[^0-9]", "");
            }
            bool isComplete = controller.LogWork(ctx, int.Parse(workOrder), material, int.Parse(amount));
            if (isComplete)
            {
                await updateBoard(ctx);
            }
        }

        [Command("subscribe")]
        public async Task Enlist(CommandContext ctx, string type = null)
        {
            if(type == null)
            {
                var interactivity = ctx.Client.GetInteractivity();
                await ctx.RespondAsync("To What type of tasks would you like to subscribe? currently your options are: 'medical' ");
                var typemsg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(2));
                DispatchController.Enlist(ctx, typemsg.Result.Content);

            }
            else
            {
                DispatchController.Enlist(ctx, type);
            }

        }

        [Command("GetContributions")]
        public async Task GetContributions(CommandContext ctx, int? max = null)
        {
            await ctx.Channel.SendMessageAsync(embed: TransactionController.GetContributions(ctx.Guild, max));
        }

        [Command("GetContributions")]
        public async Task GetContributions(CommandContext ctx, DiscordMember member)
        {
            await ctx.Channel.SendMessageAsync(TransactionController.GetContributions(ctx.Guild, member));
        }

        [Command("wipe-bank")]
        public async Task WipeBank(CommandContext ctx)
        {
            try
            {
                TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "wipe-bank", ctx);
                BankController bankController = new BankController();

                var bankers = await GetMembersWithRolesAsync("Banker", ctx.Guild);

                if (bankers.Contains(ctx.Member.Id))
                {
                    var interactivity = ctx.Client.GetInteractivity();
                    await ctx.RespondAsync("Are you sure you want to continue? This Cannot be undone");
                    var confirmMsg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5));

                    if (confirmMsg.Result.Content.ToLower() == "yes")
                    {
                        bankController.WipeBank(ctx.Guild);
                        TransactionController.WipeTransactions(ctx.Guild);
                        LoanController.WipeLoans(ctx);


                        TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "wipe-bank-success", ctx);
                        await ctx.RespondAsync("your org balance and transactions have been set to 0. All Loans have been completed");
                    }
                }
                else
                {
                    await ctx.RespondAsync("Nice Try Bub but you have to be a banker to perform a wipe");
                }
            } catch(Exception e)
            {
                await ctx.RespondAsync($"Send Wnr the following error {e.Message}");
            }

           
        }

        [Command("run-message")]
        public async Task runScheduledMessage(CommandContext ctx, string channel, int id)
        {
            try
            {
                var channels = await ctx.Guild.GetChannelsAsync();
                DiscordChannel chan = null;
                foreach (var iChan in channels)
                {
                    if (iChan.Name == channel)
                    {
                        chan = iChan;
                        break;
                    }
                }

                var msgs = await( new SkynetProtocol().RunMessage(id, ctx));
                foreach (var msg in msgs)
                {
                    await chan.SendMessageAsync(msg.ToString());
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [Command("getreactions")]
        public async Task GetId(CommandContext ctx)
        {
            TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "get-reactions", ctx);

            var interactivity = ctx.Client.GetInteractivity();
            var test =await  ctx.RespondAsync("you pick one! :poop: :100:");
            await test.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":poop:"));
            await test.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":100:"));

            List<DiscordEmoji> emojis = new List<DiscordEmoji>
            {
                DiscordEmoji.FromName(ctx.Client, ":poop:"),
                DiscordEmoji.FromName(ctx.Client, ":100:")
            };

            Thread.Sleep(500);
            var test2 = await interactivity.WaitForReactionAsync(i => i.Emoji == emojis[0] || i.Emoji == emojis [1], timeoutoverride: TimeSpan.FromSeconds(5));

            try
            {
                if (test2.Result.Emoji.Name == "💩")
                {
                    await test.RespondAsync("You know what screw you too buddy");
                }
                else if (test2.Result.Emoji.Name == "💯")
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

        public async Task SkynetProtocol(MessageCreateEventArgs e)
        {
            string sp = new SkynetProtocol().ResponsePicker(e.Message.Content);
            if (sp != "")
            {
                await e.Channel.SendMessageAsync(sp);
            }


        }

        [Command("echo")]
        public async Task Echo(CommandContext ctx, string channel, params string[] message)
        {
            var channels = await ctx.Guild.GetChannelsAsync();
            DiscordChannel chan = null;
            foreach (var iChan in channels)
            {
                if (iChan.Name == channel)
                {
                    chan = iChan;
                    break;
                }
            }

            await chan.SendMessageAsync(string.Join(" ", message));
        }

        [Command("skynet")]
        public async Task SkynetTest(CommandContext ctx, int number, string message)
        {
            string sp = new SkynetProtocol().ResponsePicker(number, message);
            await ctx.Channel.SendMessageAsync(sp);
        }

        [Command("rescue")]
        public async Task RequestRescue(CommandContext ctx)
        {
            //getting orgs that have medical
            var orgs = DispatchController.GetRescueOrgs();

            List<DiscordMessage> messages = new List<DiscordMessage>();
            var dispatchInter = ctx.Client.GetInteractivity();

            var handlemsg = await ctx.RespondAsync("Please post your RSI handle");
            var handleResp = await dispatchInter.WaitForMessageAsync(d => d.Author.Id == ctx.User.Id);

            var locationMessage = await ctx.RespondAsync("Please provide your approximate location - Planetary body and distance to nearest landmarks");
            var locationResp = await dispatchInter.WaitForMessageAsync(d => d.Author.Id == ctx.User.Id);

            var otherMessage = await ctx.RespondAsync("Other Pertinent Information: Injury? Hostiles Present? Time remaining?");
            var otherResponse = await dispatchInter.WaitForMessageAsync(d => d.Author.Id == ctx.User.Id);


            if (handleResp.Result.Content != null && locationResp.Result.Content != null)
            {
                await handlemsg.DeleteAsync();
                await handleResp.Result.DeleteAsync();
                await locationResp.Result.DeleteAsync();
                await locationMessage.DeleteAsync();
                await otherMessage.DeleteAsync();
                await otherResponse.Result.DeleteAsync();

                await ctx.RespondAsync("Please hold while I find an available unit. Look for a Direct message with your rescue unit information - This could take several minutes");
            }

            //send a message to those discords
            var qjm = orgs.First(x => x.OrgName == "MultiCorp");
            var qjmmsg = await DispatchController.SendOrgMessage(ctx, qjm, locationResp.Result.Content);
            orgs.Remove(qjm);

            //wait until someone accepts
            DiscordMember acceptedUser = null;
            await qjmmsg.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":rotating_light:"));
            var qjmAcceptedInter = ctx.Client.GetInteractivity();
            var qjmAccepted = await qjmAcceptedInter.WaitForReactionAsync(x => x.User.Id != qjmmsg.Author.Id && x.Emoji == DiscordEmoji.FromName(ctx.Client, ":rotating_light:"), TimeSpan.FromMinutes(4));
            if (qjmAccepted.Result == null)
            {
                Random rand = new Random();
                while(acceptedUser == null)
                {
                    await qjmmsg.DeleteAsync();
                    await qjmmsg.Channel.SendMessageAsync("Since no one was available within the 4 minute time limit the dispatch was sent to another provider");
                    var org = orgs[rand.Next(orgs.Count)];

                    var msg = await DispatchController.SendOrgMessage(ctx, org, locationResp.Result.Content);
                    await msg.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":rotating_light:"));
                    var acceptedInter = ctx.Client.GetInteractivity();
                    var accepted = await acceptedInter.WaitForReactionAsync(x => x.User.Id != qjmmsg.Author.Id && x.Emoji == DiscordEmoji.FromName(ctx.Client, ":rotating_light:"), TimeSpan.FromMinutes(4));
                    if (accepted.Result != null)
                    {
                        acceptedUser = await accepted.Result.Guild.GetMemberAsync(accepted.Result.User.Id);
                    }
                    else
                    {
                        orgs.Remove(org);
                        await msg.Channel.SendMessageAsync("Since no one was available within the 4 minute time limit the dispatch was sent to another provider");
                        await msg.DeleteAsync();
                    }
                }
            }
            else
            {
                acceptedUser = await qjmAccepted.Result.Guild.GetMemberAsync(qjmAccepted.Result.User.Id);
                await qjmmsg.DeleteAsync();
            }

            //delete all the message from the discord channels

            //create a dm
            if (acceptedUser != null)
            {
                var acceptorDm = await acceptedUser.CreateDmChannelAsync();
                await acceptorDm.SendMessageAsync($"You've accepted a rescue from member please send them a friend request ASAP: {ctx.Member} " +
                    $"\n RSI Handle: {handleResp.Result.Content} " +
                    $"\n Location: {locationResp.Result.Content}" +
                    $"\n Other Pertinent Info: {otherResponse.Result.Content}");
                
                var requestorDm = await ctx.Member.CreateDmChannelAsync();
                await requestorDm.SendMessageAsync($"Please stand by, User: {acceptedUser.Username} from {acceptedUser.Guild.Name} has accepted and is on his way. Please look for discord and RSI invites");
                await requestorDm.SendMessageAsync($"Would you like an invite to {acceptedUser.Guild.Name}? " +
                    $"\nYes = you will be put in a specific patient channel " +
                    $"\nNo = you agree to wait for a discord/RSI friend request from {acceptedUser.Username}");
                var confirm = await requestorDm.GetNextMessageAsync(timeoutOverride: TimeSpan.FromMinutes(4));

                if(confirm.Result.Content != null && confirm.Result.Content.ToLower() == "yes")
                {
                    var invites = (await acceptedUser.Guild.GetInvitesAsync()).ToList();
                
                    var channel = (await acceptedUser.Guild.GetChannelsAsync()).First(x => x.Name == "medical-assistance");
                    var inv = invites.FirstOrDefault(x => x.Channel.Name == "medical-assistance");

                    await requestorDm.SendMessageAsync(inv.ToString());
                    await acceptorDm.SendMessageAsync($"{ctx.Member.Username} will be joining you server as a 'patient'");
                    

                    bool userInChannel = false;
                    int i = 0;


                    try
                    {
                        while (userInChannel == false || i < 300000)
                        {
                            var users = await acceptedUser.Guild.GetAllMembersAsync();
                            var use = users.FirstOrDefault(x => x.Id == ctx.Member.Id);
                            if (use != null)
                            {
                                var patient = acceptedUser.Guild.Roles.FirstOrDefault(x => x.Value.Name == "Patient").Value;
                                await use.GrantRoleAsync(patient, "New Patient");
                                userInChannel = true;
                            }
                            i++;
                        }
                    } catch(Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                else
                {
                    await acceptorDm.SendMessageAsync($"{ctx.Member.Username} does not wish to join your discord, please reach out to them directly");
                }

                DispatchController.LogDispatch(ctx.Member, acceptedUser);
            }
            else
            {
                Console.WriteLine("Well shit, that didn't work");
            }
        }

        [Command("updateId")]
        public Task UpdateDiscordid(CommandContext ctx)
        {
            OrgController.UpdateDiscordId(ctx.Guild);

            return Task.CompletedTask;
        }

        private async Task<Tuple<bool, WorkOrders>> AcceptDispatch(CommandContext ctx, string type, int? id=null)
        {
            var controller = new WorkOrderController();
            var interactivity = ctx.Client.GetInteractivity();
            var wOrder = await controller.GetWorkOrders(ctx, type, id);
            var msg = await ctx.RespondAsync(embed: wOrder.Item1);

            var confirmDeny = await ctx.RespondAsync("Please respond with 'accept' or 'deny'"); 

            var confirmMsg = await interactivity.WaitForMessageAsync(r => (r.Content.ToLower().Contains("accept") || r.Content.ToLower().Contains("deny")) && r.Author.Id == ctx.User.Id, timeoutoverride: TimeSpan.FromMinutes(5));

        
                if (confirmMsg.Result.Content.ToLower().Contains("accept"))
            {
                return new Tuple<bool, WorkOrders>(true, wOrder.Item2);
            }
            else
            { 
                await msg.DeleteAsync();
                await confirmDeny.DeleteAsync();
                return new Tuple<bool, WorkOrders>(false, null);
            }

        }

        private async Task AddWorkOrder(CommandContext ctx)
        {
            var interactivity = ctx.Client.GetInteractivity();
            await ctx.RespondAsync("Please add the title");
            string title = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5))).Result.Content;
            await ctx.RespondAsync("Please add a Description");
            string description = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5))).Result.Content;
            await ctx.RespondAsync("Please add a type: trading, mining shipping or military");
            string workOrdertype = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5))).Result.Content;
            await ctx.RespondAsync("Please add a location");
            string location = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5))).Result.Content;

            await ctx.RespondAsync("How many requirements will it have?");
            int reqCount = int.Parse((await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5))).Result.Content);
            List<Tuple<string, int>> req = new List<Tuple<string, int>>();
            for (int i = 0; i < reqCount; i++)
            {
                string matMessage = "";
                string amntMessage = "";
                if (workOrdertype.Contains("military"))
                {
                    matMessage = "What type of Objectives will they be completing?";
                    amntMessage = "How many Objectives will they be completing?";
                }
                else
                {
                    matMessage = $"What is the material they will be {workOrdertype}?";
                    amntMessage = $"What how much of the material will they be {workOrdertype}?";
                }
                await ctx.RespondAsync(matMessage);
                string reqMaterial = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5))).Result.Content;
                await ctx.RespondAsync(amntMessage);
                int reqAmount = int.Parse((await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5))).Result.Content);

                req.Add(new Tuple<string, int>(reqMaterial, reqAmount));
            }

            var controller = new WorkOrderController();
            await controller.AddWorkOrder(ctx, title, description, workOrdertype, location, req);

            await ctx.RespondAsync("Work Order has been added to the dispatch list");
            await updateBoard(ctx); 
        }

        private async Task FleetRequest(CommandContext ctx)
        {
            await ctx.RespondAsync("What is the Make and Model of the ship you're requesting");
            var interactivity = ctx.Client.GetInteractivity();
            var item = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5))).Result.Content;

            await ctx.RespondAsync("What is the price of the ship in aUEC");
            var price = int.Parse((await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5))).Result.Content);
            await ctx.RespondAsync("Please provide an image url of the ship you're requestion");
            var image = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5))).Result.Content;
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
            var interactivity = ctx.Client.GetInteractivity();
            var item = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5))).Result.Content;
            await ctx.RespondAsync("How many credits you put towards the ship");
            var credits = int.Parse((await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5))).Result.Content);

            await ctx.RespondAsync("Waiting for Banker to confirm the transfer");
            var bankers = await GetMembersWithRolesAsync("Banker", ctx.Guild);
            var confirmMsg = await interactivity.WaitForMessageAsync(xm => bankers.Contains(xm.Author.Id), TimeSpan.FromMinutes(10));
            if (confirmMsg.Result.Content.ToLower().Contains("yes")
                || confirmMsg.Result.Content.ToLower().Contains("confirm")
                || confirmMsg.Result.Content.ToLower().Contains("approve"))
            {
                BankTransaction trans = new BankTransaction("deposit", ctx.Member, ctx.Guild, credits);
                bankController.Deposit(trans);
                bankController.UpdateTransaction(trans);
                var xp = MemberController.UpdateExperiencePoints("credits for ships" ,trans);
                FleetController.UpdateFleetItemAmount(int.Parse(item), credits);
                await ctx.RespondAsync($"Your funds have been accepted and you've been credited the transaction.\n Your org experience is now {FormatHelpers.FormattedNumber(xp.ToString())}");
            }

        }

        private async Task LoanFund(CommandContext ctx, string bank = null)
        {
            try {
                var bankers = await GetMembersWithRolesAsync("Banker", ctx.Guild);

                await ctx.RespondAsync("Which Loan would you like to fund", embed: LoanController.GetWaitingLoansEmbed(ctx.Guild));

                var interactivity = ctx.Client.GetInteractivity();
                var loanIdMsg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5));

                Loans loan = null;

                if (bankers.Contains(ctx.Member.Id) && bank == "bank" && !loanIdMsg.TimedOut)
                {
                    var confirmEmojis = ConfirmEmojis(ctx);

                    var approval = await ctx.RespondAsync("Are you sure you want to fund the loan with Bank funds? Please respond with 'yes' or 'approve'");
                    var confirmMsg = await interactivity.WaitForMessageAsync(xm => bankers.Contains(xm.Author.Id), TimeSpan.FromMinutes(10));
                    if (confirmMsg.Result.Content.ToLower().Contains("yes")
                        || confirmMsg.Result.Content.ToLower().Contains("confirm")
                        || confirmMsg.Result.Content.ToLower().Contains("approve"))
                    {
                        loan = await LoanController.FundLoan(ctx, ctx.Member, ctx.Guild, loanIdMsg.Result, true);
                        await ctx.RespondAsync($"Congratulations " +
                            $"{(await MemberController.GetDiscordMemberByMemberId(ctx, loan.ApplicantId)).Mention}! \n" +
                            $" {ctx.Guild.Name} is willing to fund your loan!" +
                            $" Reach out to a Guild banker to receive your funds");
                    }
                    else
                    {
                        await ctx.RespondAsync("Loan funding with bank credits has been cancelled");
                    }

                }
                else if(loanIdMsg.TimedOut)
                {
                    await ctx.RespondAsync("Loan Request Timed out. Please start loan fund request again.");
                }
                else
                {
                    loan = await LoanController.FundLoan(ctx, ctx.Member, ctx.Guild, loanIdMsg.Result);
                    await ctx.RespondAsync($"Congratulations " +
                    $"{(await MemberController.GetDiscordMemberByMemberId(ctx, loan.ApplicantId)).Mention}! \n" +
                    $"{(await MemberController.GetDiscordMemberByMemberId(ctx, loan.FunderId.GetValueOrDefault())).Mention}is willing to fund your loan!" +
                    $" Reach out to them to receive your funds");
                }
            } catch(Exception e)
            {
                tHelper.LogException("Loan Fund Exception", e);
                Console.WriteLine(e);
                await ctx.RespondAsync("Sorry something went wrong with your request");
            }
        }

        private async Task LoanComplete(CommandContext ctx, string id = null)
        {
            if(id == null)
            {
                await ctx.RespondAsync("Which Loan would you like to complete", embed: LoanController.GetFundedLoansEmbed(ctx.Guild));

                var interactivity = ctx.Client.GetInteractivity();
                id = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5))).Result.Content;

            }

            var loan = LoanController.CompleteLoan(int.Parse(id));

            await ctx.RespondAsync($"Congratulations " +
                $"{(await MemberController.GetDiscordMemberByMemberId(ctx, loan.ApplicantId)).Mention}! \n" +
                $"You've paid off your loan and you're debt free! For now :money_mouth:");
        }

        private async Task<DiscordEmbed> LoanView(CommandContext ctx)
        {
            var plsHold = await ctx.RespondAsync("Getting your Loan Information, please hold");
            var embed = LoanController.GetLoanEmbed(ctx.Guild);
            await ctx.RespondAsync(embed: LoanController.GetLoanEmbed(ctx.Guild));
            await plsHold.DeleteAsync();
            return embed;
        }

        private async Task LoanPayment(CommandContext ctx)
        {
            Loans loan = null;
            var bankers = await GetMembersWithRolesAsync("Banker", ctx.Guild); 
            var pullingMsg = await ctx.RespondAsync("Pulling up your loan info now.");
            try
            {
                int memberId = MemberController.GetMemberbyDcId(ctx.Member, ctx.Guild).UserId;
                var loans = LoanController.GetLoanByApplicantId(memberId);

                if (loans != null)
                {
                    var interactivity = ctx.Client.GetInteractivity();

                    if(loans.Count > 1)
                    {
                        var loanCntMgs = await ctx.RespondAsync($"You have {loans.Count} outstanding loans. What is the is the id of the loan which you'd like to make a payment?");
                        var viewEmbed = await LoanView(ctx);
                        var loanIdMsg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5));
                        loan = loans.Find(x => x.LoanId == int.Parse(loanIdMsg.Result.Content));

                        loanIdMsg.Result.DeleteAsync();
                    }
                    else
                    {
                        loan = loans[0];
                    }

                    
                    var balmsg = await ctx.RespondAsync($"Your current balance is {loan.RemainingAmount} much of your loan would you like to repay");
                    var amountmsg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5));

                    if (loan.FunderId == 0)
                    {
                        var confirmMsg = await ctx.RespondAsync($"Waiting for a Bankerconfirm this payment, you have 10 minutes to confirm, please type 'yes' or 'confirm'");
                        var confirm = await interactivity.WaitForMessageAsync(xm => bankers.Contains(xm.Author.Id));
                        if (bankers.Contains(confirm.Result.Author.Id) && confirm.Result.Content.Contains("yes") || confirm.Result.Content.Contains("confirm"))
                        {
                            LoanController.MakePayment(loan.LoanId, int.Parse(amountmsg.Result.Content));
                            await ctx.RespondAsync($"Payment of {FormatHelpers.FormattedNumber(amountmsg.Result.Content)} has been confirmed for loan: {loan.LoanId}. The new balance is {LoanController.GetLoanById(loan.LoanId).RemainingAmount}");
                        }

                        pullingMsg.DeleteAsync();
                        balmsg.DeleteAsync();
                        amountmsg.Result.DeleteAsync();
                        confirmMsg.DeleteAsync();
                    }
                    else
                    {
                        var fundedMember = await ctx.Guild.GetMemberAsync(ulong.Parse(MemberController.GetMemberById(loan.FunderId.GetValueOrDefault()).DiscordId));
                        var confirmMsg = await ctx.RespondAsync($"{fundedMember.Mention} Please confirm this payment, you have 10 minutes to confirm, please type 'yes' or 'confirm'");
                        var confirm = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == fundedMember.Id, TimeSpan.FromMinutes(10)));
                        if (confirm.Result.Content.Contains("yes") || confirm.Result.Content.Contains("confirm"))
                        {
                            LoanController.MakePayment(loan.LoanId, int.Parse(amountmsg.Result.Content));
                            await ctx.RespondAsync($"Payment of {FormatHelpers.FormattedNumber(amountmsg.Result.Content)} has been confirmed for loan - {loan.LoanId}: The new balance is {LoanController.GetLoanById(loan.LoanId).RemainingAmount}");
                        }

                        pullingMsg.DeleteAsync();
                        balmsg.DeleteAsync();
                        amountmsg.Result.DeleteAsync();
                        confirmMsg.DeleteAsync();
                        confirm.Result.DeleteAsync();
                    }

                }
                else
                {
                    TelemetryHelper.Singleton.LogEvent("BOT TASK", "task-loan-find-not", ctx);
                    await ctx.RespondAsync("Could not find loan");
                }
            } catch (Exception e)
            {
                TelemetryHelper.Singleton.LogException("task-loan-pay", e);
                Console.WriteLine(e);
            }

        }

        private async Task<List<ulong>> GetMembersWithRolesAsync(string roleLevel, DiscordGuild guild)
        {
            try
            {
                var bankerRole = guild.Roles.FirstOrDefault(x => x.Value.Name == roleLevel);
                var members = await guild.GetAllMembersAsync();
                List<ulong> bankersIds = new List<ulong>();
                foreach (var member in members)
                {
                    if (member.Roles.Contains(bankerRole.Value))
                    {
                        bankersIds.Add(member.Id);
                    }
                }
                return bankersIds;
            } catch (Exception e)
            {
                Console.Write(e);
            }
            return null;
        }
        private async Task LoanRequest(CommandContext ctx)
        {
            var interactivity = ctx.Client.GetInteractivity();
           
            await ctx.RespondAsync("Thank you for banking with MultiBot. \nI'll need some info to process your request. \n\nHow much are you looking to borrow?");
            var amountmsg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5));
            try
            {
                int amount = int.Parse(amountmsg.Result.Content);
                await ctx.RespondAsync("Excellent! Do you want to offer interested back? Please type 'percent' for a calculated incentive, 'flat' for a flat return, or 'none' for a no interest loan");

                var typemsg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5));
                string type = "";
                switch (typemsg.Result.Content.ToLower())
                {
                    case string a when a.Contains("percentage"):
                        type = "percent";
                        break;
                    case string b when b.Contains("flat"):
                        type = "flat";
                        break;
                    case string b when b.Contains("none"):
                        type = "none";
                        break;
                    case string b when b.Contains("0"):
                        type = "none";
                        break;
                    default:
                        await ctx.RespondAsync("Sorry I didn't get that. You will need to start over. Please type !Loan Requests and provide a valid type. types must include 'flat', 'percentage', or 'none'.");
                        break;
                }

                if(type == "percent")
                {
                    await ctx.RespondAsync("What % interest are you offering. Please use whole numbers: e.g. 3");
                    var interest = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5));
                    
                    var interestamount = LoanController.CalculateInterest(amount, int.Parse(interest.Result.Content));
                    await ctx.RespondAsync("Please hold your application is being processed");
                    LoanController.AddLoan(ctx.Member, ctx.Guild, amount, interestamount);
                    await ctx.RespondAsync($"Your Loan of {amount} with a total repayment of {amount + interestamount} is waiting for funding");
                    TelemetryHelper.Singleton.LogEvent("BOT TASK", "loan-request-details", ctx);//seeing if it gets here
                }
                else if(type == "flat")
                {
                    await ctx.RespondAsync("What amount interest are you offering. Please use whole numbers: e.g. 20000");
                    var interest = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5));
                    var interestAmount = int.Parse(interest.Result.Content);
                    await ctx.RespondAsync("Please hold your application is being processed");

                    try
                    {
                        LoanController.AddLoan(ctx.Member, ctx.Guild, amount, interestAmount);
                        await ctx.RespondAsync($"Your Loan of {amount} with a total repayment of {amount + interestAmount} is waiting for funding");
                    }
                    catch (Exception e)
                    {
                        TelemetryHelper.Singleton.LogException("task-loan-add", e);
                        Console.WriteLine(e);
                        await ctx.RespondAsync("I'm sorry, but an error has occured please notify your banker.");
                    }
                }
                else if(type == "none")
                {
                    await ctx.RespondAsync("Please hold your application is being processed");

                    try
                    {
                        LoanController.AddLoan(ctx.Member, ctx.Guild, amount, 0);
                        await ctx.RespondAsync($"Your Loan of {amount} with a total repayment of {amount + 0} is waiting for funding");
                    }
                    catch (Exception e)
                    {
                        TelemetryHelper.Singleton.LogException("task-loan-add", e);
                        Console.WriteLine(e);
                        await ctx.RespondAsync("I'm sorry, but an error has occured please notify your banker.");
                    }
                }

            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                TelemetryHelper.Singleton.LogException("task-loan-request", e);
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