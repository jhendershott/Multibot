using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace multicorp_bot
{
    public class Commands: BaseCommandModule
    {
        readonly MemberController MemberController;
        readonly TransactionController TransactionController;
        readonly LoanController LoanController;
        readonly FleetController FleetController;
        readonly OrgController OrgController;
        readonly WorkOrderController WorkOrderController;
        readonly DispatchController DispatchController;
        readonly FactionController FactionController;
        readonly BankController BankController;
        //Dan: I added variables here cause i didnt know how else to have them persist between commands and calls and stuff, idk if theres a better way
        //Other code added is CreateBoard(), UpdateBoard(), !view and then some changes to AcceptDispatch and GetWorkOrders so I could accomodate specific fishing of the iD when asked with !view.
        //
        //
        public Commands()
        {
            MemberController = new MemberController();
            TransactionController = new TransactionController();
            LoanController = new LoanController();
            FleetController = new FleetController();
            OrgController = new OrgController();
            WorkOrderController = new WorkOrderController();
            DispatchController = new DispatchController();
            FactionController = new FactionController();
            BankController = new BankController();
            PermissionsHelper.LoadPermissions();
        }

        [Command("test")]
        public async Task Test(CommandContext ctx)
        {
            await ctx.RespondAsync($"I'm here {ctx.Member.Nickname}");
        }

        [Command("handle")]
        public async Task UpdateHandle(CommandContext ctx, params string [] handle)
        {
            DiscordMember member = null;
            try
            {
                string handlestr = string.Join(" ", handle);
                string newNick = null;

                member = ctx.Member;

                MemberController.UpdateMemberName(ctx, member.Nickname, handlestr, ctx.Guild);
                await member.ModifyAsync(x => x.Nickname = newNick);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [Command("help")]
        public async Task Help(CommandContext ctx, string helpCommand = null)
        {
            if (helpCommand == null)
            {
                List<string> helpTypes = new List<string>()
                {
                    "Bank", "Loans", "Handle", "Fleet", "Dispatch", "Log", "Wipe"
                };

                DiscordComponent[] buttons = new DiscordComponent[helpTypes.Count];
                for (int i = 0; i < helpTypes.Count(); i++)
                {
                    buttons[i] = new DiscordButtonComponent(ButtonStyle.Primary, $"help-{helpTypes[i]}", helpTypes[i]);
                }

                try
                {
                    DiscordMessageBuilder msg = new DiscordMessageBuilder()
                        .WithContent("What commands would you like help with? \nNext time you can also type !help {command} such as !help bank")
                        .AddComponents(buttons.Take(5))
                        .AddComponents(buttons.Skip(5));
                    await ctx.RespondAsync(msg);
                } catch(Exception e)
                {
                    ErrorController.SendError(ctx.Channel, e.Message, ctx.Guild);
                }
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
            try
            {
                OrgController.AddOrg(ctx.Guild);
                BankController.AddBankEntry(ctx.Guild);
                await ctx.RespondAsync("You're all setup and ready to go");
                await ctx.RespondAsync("Please Add the following roles: \n - bot-admin\n - Banker");
                await ctx.RespondAsync("Please Add the following channels: \n - job-board (public)\n - bot-error (private)");
            }
            catch (Exception e)
            {
               await ctx.RespondAsync($"Error on setup {e}");
            }
        }

        [Command("bank")]
        public async Task Bank(CommandContext ctx)
        {
            await ctx.RespondAsync($"Please run the command the commands !bank deposit, or Bankers Only - !bank withdraw, or !bank reconcile");
        }

        [Command("bank")]
        public async Task Bank(CommandContext ctx, string command)
        {
            BankController BankController = new BankController();
            string[] args = Regex.Split(ctx.Message.Content, @"\s+");
            var interactivity = ctx.Client.GetInteractivity();
            var bankers = await GetMembersWithRolesAsync("Banker", ctx.Guild);

            try
            {
                switch (command.ToLower())
                {
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
                            await updateBankBoard(ctx.Guild, ctx.Channel);
                            break;
                        }
                    
                        catch (Exception e)
                        {
                            ErrorController.SendError(ctx.Channel, e.Message, ctx.Guild);
                            break;
                        }
                    case "deposit" or "withdraw":
                        await ctx.RespondAsync($"How much would you like to {command}? Please use whole numbers");

                        var confirmMsg = await interactivity.WaitForMessageAsync(r => r.Author == ctx.User, timeoutoverride: TimeSpan.FromMinutes(20));
                        int n;
                        if (int.TryParse(confirmMsg.Result.Content, out n))
                        {
                            await Bank(ctx, command, confirmMsg.Result.Content);
                            break;
                        }
                        else
                        {
                            await ctx.RespondAsync("Please Try again and use a whole number in your amount");
                            break;
                        }

                }
            }
            catch (Exception e)
            {
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
                                transaction = BankController.GetBankAction(ctx, "deposit", amount, user, "credit");
                            }
                            else if (type.Contains("merit"))
                            {
                                transaction = BankController.GetBankAction(ctx, "deposit", amount, user, "merit");
                                isCredit = false;
                            }

                            newBalance = await BankController.Deposit(transaction);
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
                Console.WriteLine(e);
                await ctx.Channel.SendMessageAsync($"Send WNR the following error: {e}");
            }
        }

        [Command("bank")]
        public async Task Bank(CommandContext ctx, string command, string amount)
        {
            BankController BankController = new BankController();
            string[] args = Regex.Split(ctx.Message.Content, @"\s+");
            var interactivity = ctx.Client.GetInteractivity();
            var bankers = await GetMembersWithRolesAsync("Banker", ctx.Guild);

         
            await ctx.RespondAsync($"Would you like to {command} credits or merits?");
            var confirmMsg = await interactivity.WaitForMessageAsync(r => r.Author == ctx.User, timeoutoverride: TimeSpan.FromMinutes(20));
            if (!confirmMsg.Result.Content.ToLower().Contains("merit") && !confirmMsg.Result.Content.ToLower().Contains("credit"))
            {
                await ctx.RespondAsync("Please specify credits or merits e.g. !bank deposit 1000 merits || !bank deposit 1000 credits");
            }
            else
            { 
                await BankBundle(ctx, command, amount, confirmMsg.Result.Content.ToLower());
            }           
        }

        [Command("bank")]
        public async Task Bank(CommandContext ctx, string command, string amount, string type)
        {
            await BankBundle(ctx, command, amount, type);
        }


        [Command("updateBank")]
        public async Task updateBank(CommandContext ctx)
        {
            await updateBankBoard(ctx.Guild, ctx.Channel);
        }

        public async Task updateBankBoard(DiscordGuild guild, DiscordChannel channel)
        {
            var isRp = OrgController.isRpOrg(guild);
            DiscordChannel bankChannel;
            if (isRp)
            {
                bankChannel = (await guild.GetChannelsAsync()).FirstOrDefault(x => x.Name == "bank" && x.Parent.Name == "Out Of Character");
                await updateRpBankBoard(guild, channel);
            }
            else
            {
                bankChannel = (await guild.GetChannelsAsync()).FirstOrDefault(x => x.Name == "bank");
            }
                
            if (bankChannel == null )
            {
                await channel.SendMessageAsync("For a cleaner and more readable experience you must create a channel called 'bank' (in the 'Out of Character' category if rp org");
            }
            else
            {
                var msgs = await bankChannel.GetMessagesAsync();

                if (msgs.Count > 0)
                {
                    await bankChannel.DeleteMessagesAsync(msgs);
                }

                try
                {
                    Console.Write($"Balance Command accepts for org {guild.Name}");
                    await bankChannel.SendMessageAsync(BankController.GetBankBalanceEmbed(guild));
                }
                catch (Exception e)
                {
                    ErrorController.SendError(channel, e.Message, guild);
                }
            }
        }

        public async Task updateRpBankBoard(DiscordGuild guild, DiscordChannel channel)
        {
            DiscordChannel bankChannel = (await guild.GetChannelsAsync()).FirstOrDefault(x => x.Name == "bank" && x.Parent.Name == "Out Of Character");
            DiscordChannel icBankChannel = (await guild.GetChannelsAsync()).FirstOrDefault(x => x.Name == "bank" && x.Parent.Name == "In Character");
        
            if (bankChannel == null || icBankChannel == null)
            {
                await channel.SendMessageAsync("For a cleaner and more readable experience you must create a channel called bank in In Character and Out Of Character");
            }
            else
            {
                var msgs = await bankChannel.GetMessagesAsync();
                var icMsgs = await icBankChannel.GetMessagesAsync();

                if (msgs.Count > 0)
                {
                    await bankChannel.DeleteMessagesAsync(msgs);
                }


                if (icMsgs.Count > 0)
                {
                    await icBankChannel.DeleteMessagesAsync(icMsgs);
                }

                try
                {
                    Console.Write($"Balance Command accepts for org {guild.Name}");
                    await bankChannel.SendMessageAsync(BankController.GetBankBalanceEmbed(guild));
                    await icBankChannel.SendMessageAsync(BankController.GetRpBankBalanceEmbed(guild));
                }
                catch (Exception e)
                {
                    ErrorController.SendError(channel, e.Message, guild);
                }
            }
        }

        [Command("fleet")]
        public async Task Fleet(CommandContext ctx, string arg)
        {
            switch (arg.ToLower()){
                case "view":
                    await ctx.RespondAsync(embed: new FleetController().GetFleetRequests(ctx.Guild));
                    break;
                case "request":
                    await FleetRequest(ctx);
                    break;
                case "fund":
                    await FundFleet(ctx);
                    break;
                case "complete":
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
            switch (command.ToLower())
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
        
        [Command("loan")]
        public async Task Loan(CommandContext ctx, string command, string qualifier)
        {
            switch (command.ToLower())
            {
                case "fund":
                    await LoanFund(ctx, qualifier);
                    break;
                case "complete":
                    await LoanComplete(ctx, qualifier);
                    break;
                //case "add": LoanController.AddLoan(ctx.Member, ctx.Guild, 50000, 1000);
                //    break;
                default:
                    await ctx.RespondAsync("Options for loans is 'request', 'view', 'payment', 'fund', and 'complete'");
                    break;
            }
        }      

        [Command("dispatch")]
        public async Task Dispatch(CommandContext ctx, string type = null)
        {
            try
            {
                WorkOrderController controller = new WorkOrderController();

                switch (type.ToLower())
                {
                    case "add":
                        await AddWorkOrder(ctx);
                        break;
                    case "view":
                        await ctx.Channel.SendMessageAsync(embed: WorkOrderController.GetWorkOrderByMember(ctx));
                        break;
                    case "log":
                        await Log(ctx);
                        break;
                    default:
                        await ctx.RespondAsync("Please use !dispact add|view|log");
                        break;
                };
            }
            catch(Exception e)
            {
                ErrorController.SendError(ctx.Channel, e.Message, ctx.Guild);
            }
        }

        [Command("addFaction")]
        public async Task AddFaction(CommandContext ctx, string factionName = null)
        {
            if (factionName != null)
            {
                await ctx.RespondAsync($"New faction created with {factionName} and ID: {FactionController.AddFaction(factionName, ctx.Guild)}");
            }
        }

        [Command("updateBoard")]   //command to test updating the board, we should call UpdateJobBoard() everytime we remove and add orders.
        public async Task updateBoard(CommandContext ctx, string type = null)
        {
            await UpdateJobBoard(ctx.Guild, ctx.Channel, type);
        }

        public async Task UpdateJobBoard(DiscordGuild guild, DiscordChannel channel, string type = null)
        {
            
            try
            {
                var Mychannel = (await guild.GetChannelsAsync()).FirstOrDefault(x => x.Name == "job-board");
                if (Mychannel == null)
                {
                    await channel.SendMessageAsync("For a cleaner and more readable experience you must create a channel called 'job-board'");
                }
                else
                {

                    var msgs = await Mychannel.GetMessagesAsync();

                    if (msgs.Count > 0)
                    {
                        await Mychannel.DeleteMessagesAsync(msgs);
                    }

                    if (type == null)
                    {
                        foreach(string t in WorkOrderController.Types)
                        {
                            await Mychannel.SendMessageAsync(await WorkOrderController.CreateJobBoard(guild, channel, t));
                        }
                    }
                    else if (type != null && WorkOrderController.Types.Contains(type.ToLower()))
                    {
                        await Mychannel.SendMessageAsync(await WorkOrderController.CreateJobBoard(guild, channel, type.ToLower()));
                    }
                    else
                    {
                       await channel.SendMessageAsync("Please provide a type of 'Trading', 'Shipping', 'Mining', or 'Military' ");
                    }
                }
            } catch(Exception e)
            {
                Console.WriteLine(e);
                await channel.SendMessageAsync($"Send wnr the following error {e.Message}");
            }
        }

        [Command("log")]
        public async Task Log(CommandContext ctx, string? workOrder = null, string? requirementId = null, string? amount = null)
        {
            WorkOrderController controller = new WorkOrderController();
            var interactivity = ctx.Client.GetInteractivity();
            string material;

            List<Task> tasks = new List<Task>();

            if (workOrder == null)
            {
                var m1 = await ctx.RespondAsync("What order would you like log against?");
                workOrder = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5))).Result.Content;
                tasks.Add(m1.DeleteAsync());
            }

            if (requirementId == null)
            {
                var m2 = await ctx.RespondAsync("What type or material would you like to log (the material name, not the id)");
                material = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5))).Result.Content;
                tasks.Add(m2.DeleteAsync());
            }
            else
            {
                material = controller.GetRequirementById(int.Parse(requirementId)).Material;
            }

            if (amount == null)
            {
                var m3 = await ctx.RespondAsync("How much would you like to log?");
                var msg = (await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(5))).Result.Content;
                amount = Regex.Replace(msg,  "[^0-9]", "");
                tasks.Add(m3.DeleteAsync());
            }
            bool isComplete = await controller.LogWorkAsync(ctx, int.Parse(workOrder), material, int.Parse(amount));
            if (isComplete)
            {
                await updateBoard(ctx);
            }

            if(ctx.Channel.Name == "work-log")
            {
                var msgs = await ctx.Channel.GetMessagesAsync();
                await ctx.Channel.DeleteMessagesAsync(msgs);
                await ctx.Channel.SendMessageAsync("To log work - check the job-board and make sure and get the ID\nuse !log to start the process of logging work");
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

        public async Task BankBundle(CommandContext ctx, string command = null, string amount = null, string type = null)
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
                        if (!bankers.Contains(ctx.Member.Id))
                        {
                            var confirm = await ctx.RespondAsync("Starting your deposit, please be aware if a banker is not present the transaction will timeout");

                            if (!type.ToLower().Contains("merit") && !type.ToLower().Contains("credit"))
                            {
                                await ctx.RespondAsync("Please specify credits or merits e.g. !bank deposit 1000 merits || !bank deposit 1000 credits");
                            }


                            if (type.ToLower().Contains("credit"))
                            {
                                transaction = BankController.GetBankAction(ctx, "deposit", int.Parse(amount), type: type);
                            }
                            else if (type.ToLower().Contains("merit"))
                            {
                                transaction = BankController.GetBankAction(ctx, "deposit", int.Parse(amount), type: type);
                                isCredit = false;
                            }

                            var approval = await ctx.RespondAsync("Banker please confirmed this request by replying with 'approve', 'yes' or 'confirm'");
                            var confirmMsg = await interactivity.WaitForMessageAsync(r => bankers.Contains(r.Author.Id), timeoutoverride: TimeSpan.FromMinutes(20));
                            try
                            {
                                var confirmText = confirmMsg.Result.Content.ToLower();
                                if (confirmText.Contains("yes") || confirmText.Contains("confirm") || confirmText.Contains("approve"))
                                {
                                    newBalance = await BankController.Deposit(transaction);
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
                                    await ctx.RespondAsync("Looks like someone who isn't a banker attempted to approve the transactions. " +
                                        "Only bankers can approve transactions");
                                }

                                await confirm.DeleteAsync();
                            }
                            catch (Exception)
                            {
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
                                transaction = BankController.GetBankAction(ctx, "deposit", int.Parse(amount), type: type);
                            }
                            else if (type.ToLower().Contains("merit"))
                            {
                                transaction = BankController.GetBankAction(ctx, "deposit", int.Parse(amount), type: type);
                                isCredit = false;
                            }

                            newBalance = await BankController.Deposit(transaction);
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
                            }
                        }
                        await updateBankBoard(ctx.Guild, ctx.Channel);
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
                                        transaction = BankController.GetBankAction(ctx, "withdraw", int.Parse(amount), type: type);
                                    }

                                    else if (type.ToLower().Contains("merit"))
                                    {
                                        transaction = BankController.GetBankAction(ctx, "withdraw", int.Parse(amount), type: type);
                                        isCredit = false;
                                    }
                                }
                                catch (Exception)
                                {
                                    await ctx.RespondAsync("Please confirm Credits or Merits by clicking the appropriate reaction");
                                    break;
                                }

                                newBalance = await BankController.Withdraw(transaction);
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
                            await updateBankBoard(ctx.Guild, ctx.Channel);
                            break;
                        }
                        catch (Exception e)
                        {
                            ErrorController.SendError(ctx.Channel, e.Message, ctx.Guild);
                            break;
                        }
                }

            }

            catch (Exception e)
            {
                ErrorController.SendError(ctx.Channel, e.Message, ctx.Guild);
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
            await updateBoard(ctx, workOrdertype); 
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
                await bankController.Deposit(trans);
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
                    var approval = await ctx.RespondAsync("Are you sure you want to fund the loan with Bank funds? Please respond with 'yes' or 'approve'");
                    var confirmMsg = await interactivity.WaitForMessageAsync(xm => bankers.Contains(xm.Author.Id), TimeSpan.FromMinutes(10));
                    if (confirmMsg.Result.Content.ToLower().Contains("yes")
                        || confirmMsg.Result.Content.ToLower().Contains("confirm")
                        || confirmMsg.Result.Content.ToLower().Contains("approve"))
                    {
                        loan = await LoanController.FundLoan(ctx, ctx.Member, ctx.Guild, loanIdMsg.Result, true);
                        await ctx.RespondAsync($"Congratulations " +
                            $"{(await MemberController.GetDiscordMemberByMemberId(ctx.Guild, loan.ApplicantId)).Mention}! \n" +
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
                    $"{(await MemberController.GetDiscordMemberByMemberId(ctx.Guild, loan.ApplicantId)).Mention}! \n" +
                    $"{(await MemberController.GetDiscordMemberByMemberId(ctx.Guild, loan.FunderId.GetValueOrDefault())).Mention}is willing to fund your loan!" +
                    $" Reach out to them to receive your funds");
                }
            } catch(Exception e)
            {
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
                $"{(await MemberController.GetDiscordMemberByMemberId(ctx.Guild, loan.ApplicantId)).Mention}! \n" +
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

                        await loanIdMsg.Result.DeleteAsync();
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

                        await Task.WhenAll(new Task[] {
                            Task.Run(() => pullingMsg.DeleteAsync()),
                            Task.Run(() => balmsg.DeleteAsync()),
                            Task.Run(() => amountmsg.Result.DeleteAsync()),
                            Task.Run(() => confirmMsg.DeleteAsync())
                        });
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

                        await Task.WhenAll(new Task[] {
                            Task.Run(() => pullingMsg.DeleteAsync()),
                            Task.Run(() => balmsg.DeleteAsync()),
                            Task.Run(() => amountmsg.Result.DeleteAsync()),
                            Task.Run(() => confirmMsg.DeleteAsync()),
                            Task.Run(() => confirm.Result.DeleteAsync())
                        });
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