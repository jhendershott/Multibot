using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using multicorp_bot.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace multicorp_bot.Controllers
{
    public class WorkOrderController
    {
        MultiBotDb MultiBotDb;
        public List<string> Types;

        public WorkOrderController()
        {
            MultiBotDb = new MultiBotDb();
            Types = new List<string>();
            string[] types = { "trading", "shipping", "mining", "military", "salvage" };
            Types.AddRange(types);
        }


        public double GetExpModifier(string modName)
        {
            return MultiBotDb.WorkOrderTypes.AsQueryable().Where(x => x.Name == modName).FirstOrDefault().XpModifier;
        }

        public async void AcceptWorkOrder(DiscordUser dcMem, DiscordGuild dcGuild, DiscordChannel channel, string workOrderId)
        {
            try
            {
                Member member = await new MemberController().GetMemberbyDcId(dcMem, dcGuild);
                WorkOrderMembers newMem = new WorkOrderMembers();
                newMem.MemberId = member.UserId;
                newMem.WorkOrderId = int.Parse(workOrderId);
                MultiBotDb.WorkOrderMembers.Add(newMem);
                MultiBotDb.SaveChanges();
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                ErrorController.SendError(channel, e.Message, dcGuild);
            }
        }

        public async Task<Tuple<DiscordEmbed, WorkOrders>> GetWorkOrders(CommandContext ctx, string workOrderType, int? id = null)
        {
            try
            {
                WorkOrders order = null;
                var orderType = await GetWorkOrderType(ctx.Channel, workOrderType);
                var wOrders = MultiBotDb.WorkOrders.AsQueryable().Where(x => x.OrgId == new OrgController().GetOrgId(ctx.Guild) && x.WorkOrderTypeId == orderType.Id && !x.isCompleted).ToList();

                DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
                var type = FormatHelpers.Capitalize(orderType.Name);

                builder.Title = $"{ctx.Guild.Name} {type} Dispatch";
                builder.Description = $"There are {wOrders.Count} {type} Work Orders, here's one:";
                builder.Timestamp = DateTime.Now;

               

                if (wOrders.Count > 0)
                {

                    if (id == null)
                    {
                       
                        Random rand = new Random();
                        int randOrder = rand.Next(0, wOrders.Count);
                        order = wOrders[randOrder];

                    }
                    else
                    {
                        order = MultiBotDb.WorkOrders.AsQueryable().Where(x => x.Id  == id.Value ).ToList()[0];
                    }

                    var workOrderMember = (id == null) ? GetWorkOrderMembers(order.Id) : GetWorkOrderMembers(id.Value);
                    

                    StringBuilder membersStr = new StringBuilder();
              
                    builder.AddField("Location", order.Location);
                    StringBuilder reqString = new StringBuilder();
                    foreach (WorkOrderRequirements req in GetRequirements(order.Id))
                    {
                      //  reqString.Append($"\u200b\nRequirement ID: {req.Id}\n");
                        reqString.Append($"Material: {req.Material}\n");
                        reqString.Append($"Amount: {req.Amount} Units (Units = SCU when ship mining/trading)\n");
                    }

                    if (workOrderMember.Count > 0)
                    {
                        membersStr.Append($"\n\nAccepted Members:");
                        foreach (WorkOrderMembers mem in workOrderMember)
                        {
                            var memberController = new MemberController();
                            var member = memberController.GetMemberById(mem.MemberId).Username;
                            membersStr.Append($"\n{memberController.GetMemberById(mem.MemberId).Username}");
                        }
                    }

                    builder.AddField($"Work Order Id: {order.Id} {membersStr.ToString()}", $"\n{order.Description}\n{reqString.ToString()}");

                    builder.WithFooter($"If you want to accept this order, Type !accept {order.Id}");

                    //builder.WithFooter("If you would like to accept this dispatch please respond with ✅" +
                    //  "\n to decline and see another use X" +
                    //  "\n If you are not interested in a dispatch at this time simply do nothing at all and the request will time out");
                }
                else
                {
                    builder.AddField($"Unfortnately there are no {FormatHelpers.Capitalize(orderType.Name)} Work Orders", "No open Work Orders");
                }

                builder.WithImageUrl(orderType.ImgUrl);
                return new Tuple<DiscordEmbed, WorkOrders>(builder.Build(), order);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public async Task<DiscordMessageBuilder> CreateJobBoard(DiscordGuild guild, DiscordChannel channel, string workOrderType)
        {
            try
            {
                var orderType = await GetWorkOrderType(channel, workOrderType);
                var wOrders = MultiBotDb.WorkOrders.AsQueryable().Where(x => x.OrgId == new OrgController().GetOrgId(guild) && x.WorkOrderTypeId == orderType.Id && !x.isCompleted ).ToList();
            
                DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
                var type = FormatHelpers.Capitalize(orderType.Name);

                builder.Title = $"{workOrderType.ToUpper()} Job Board";
               
                builder.Description = $"This is the {guild.Name} Job Board listing all available orders and missions issued by our members.\nFeel free to accept any missions you find worthwhile.\n-------------------------------------------------------------------";


               
                builder.Color = DiscordColor.Orange;
                if (wOrders.Count == 0)
                {
                    builder.AddField("No Work Orders", "Check back later");
                    builder.Timestamp = DateTime.Now;

                    return new DiscordMessageBuilder().AddEmbed(builder.Build());
                }
                else {
                    DiscordComponent[] buttons = new DiscordComponent[wOrders.Count];
                    for (int i = 0; i < wOrders.Count; i++)
                    {
                        var req = GetRequirements(wOrders[i].Id);
                        string reqString = "";
                        if (wOrders[i].FactionId != null)
                        {
                            builder.AddField("Faction", new FactionController().GetFactionById(wOrders[i].FactionId.GetValueOrDefault()));
                        }
                    
                        foreach(var r in req)
                        {
                            reqString = reqString + $"\n**Material:** {r.Material} \n**Amount:** {r.Amount}\n";
                        }

                        builder.AddField( ((GetWorkOrderMembers(wOrders[i].Id).Count >0)?"[ACCEPTED] ": "")+ "ID:" +wOrders[i].Id + " - " + wOrders[i].Name, $"{wOrders[i].Description} \n\n**Location:** {wOrders[i].Location} {reqString} \n-------------------------------------------------------------------");

                        buttons[i] = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, $"accept_order-{wOrders[i].Id}", $"Accept Order #{wOrders[i].Id}");
                    }
                    builder.WithFooter("If you'd like to view more about the order, Type !view <ID> \nIf you'd like to accept an order, Type Click Below <ID>\nIf you'd like to log work for an order, Type !log <ID>\n");
                    builder.Timestamp = DateTime.Now;

                    DiscordMessageBuilder msg = new DiscordMessageBuilder();
                    msg.AddEmbed(builder.Build());
                    msg.AddComponents(buttons);

                    return msg;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await channel.SendMessageAsync($"Send wnr the following error: {e}");
                return null;
            }
        }

        public DiscordEmbed GetWorkOrderByMember(CommandContext ctx)
        {
            var mbDb = new MultiBotDb();
            try
            {

                Member mem = new MemberController().GetMemberbyDcId(ctx.Member, ctx.Guild);
                List<WorkOrderMembers> memberOrders = mbDb.WorkOrderMembers.AsQueryable().Where(x => x.MemberId == mem.UserId).ToList();
                List<WorkOrders> wOrders = new List<WorkOrders>();

                foreach (var o in memberOrders)
                {
                    var order = mbDb.WorkOrders.AsQueryable().Where(x => x.Id == o.WorkOrderId).FirstOrDefault();
                    if (!order.isCompleted)
                    {
                        wOrders.Add(order);
                    }
                }

                DiscordEmbedBuilder builder = new DiscordEmbedBuilder();

                builder.Title = $"{ctx.Guild.Name} Open Work Orders - {ctx.Member.Nickname}";
                builder.Description = $"You have {wOrders.Count} open work orders?";
                builder.Timestamp = DateTime.Now;

                if (memberOrders.Count > 0 || wOrders.Count > 0)
                {
                    foreach (var order in wOrders)
                    {
                        StringBuilder reqString = new StringBuilder();
                        var workOrderMember = GetWorkOrderMembers(order.Id);
                        StringBuilder membersStr = new StringBuilder();

                        foreach (WorkOrderRequirements req in GetRequirements(order.Id))
                        {
                            reqString.Append($"\u200b\nRequirement ID: {req.Id}\n");
                            reqString.Append($"Material: {req.Material}\n");
                            reqString.Append($"Amount: {req.Amount} Units (Units = SCU when ship mining/trading)\n");
                        }
                        membersStr.Append($"\n\nAccepted Members:");

                        foreach (WorkOrderMembers acceptedMember in workOrderMember)
                        {
                            var memberController = new MemberController();
                            var member = memberController.GetMemberById(acceptedMember.MemberId).Username;
                            membersStr.Append($"\n{memberController.GetMemberById(acceptedMember.MemberId).Username}");
                        }

                        builder.AddField($"__________________________________________" +
                            $" \nWork Order Id: {order.Id} \n Location: {order.Location} {membersStr.ToString()}", $"\n{order.Description}\n{reqString.ToString()}");
                    }

                }
                else
                {
                    builder.AddField($"Unfortnately  Work Orders", "No open Work Orders");
                    return builder.Build();
                }


                builder.WithFooter("If you would like to accept this dispatch please respond with ✅" +
                    "\n to decline and see another use X" +
                    "\n If you are not interested in a dispatch at this time simply do nothing at all and the request will time out");
                builder.WithImageUrl("https://massivelyop.com/wp-content/uploads/2020/09/star-citizen-cargo-deck-768x253.png");
                return builder.Build();

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public async Task<bool> LogWorkAsync(CommandContext ctx, int id, string type, int amount)
        {
            try
            {
                bool isCompleted = true;
                var orderReqs = MultiBotDb.WorkOrderRequirements.AsQueryable().Where(x => x.WorkOrderId == id).ToList();
                var orderReq = orderReqs.AsQueryable().Where(x => x.Material.ToLower() == type.ToLower()).SingleOrDefault();

                var order = MultiBotDb.WorkOrders.AsQueryable().Where(x => x.Id == id).SingleOrDefault();
                if (order.OrgId != new OrgController().GetOrgId(ctx.Guild) || order.isCompleted)
                {
                    await ctx.RespondAsync("Please try again with a valid Work Order Id");
                    return isCompleted;
                }
                
                orderReq.Amount = orderReq.Amount - amount;
                if (orderReq.Amount <= 0)
                {
                    orderReq.isCompleted = true;
                    await ctx.RespondAsync($"Great job you have fulfilled work order for {type}");
                }
                else
                {
                    await ctx.RespondAsync($"Work Order amount remaining: {orderReq.Amount} units of {type}");
                }

                foreach (WorkOrderRequirements item in orderReqs)
                {
                    if (!item.isCompleted)
                        isCompleted = false;
                }

                MultiBotDb.WorkOrderRequirements.Update(orderReq);
                MultiBotDb.SaveChanges();

                if (isCompleted)
                {
                    order.isCompleted = true;
                    MultiBotDb.WorkOrders.Update(order);
                    if (order.FactionId != null)
                    {
                        new FactionController().AddFactionFavor(order.OrgId, order.FactionId.GetValueOrDefault());
                    }
                    await ctx.RespondAsync($"Great job you have completed the Work Order {type}");
                    CalcXpForCompletion(order);     
                }

                var xpmod = MultiBotDb.WorkOrderTypes.AsQueryable().Where(x => x.Id == orderReq.TypeId).Single().XpModifier;
                WorkOrderTypes woType = MultiBotDb.WorkOrderTypes.AsQueryable().Where(x => x.Id == orderReq.TypeId).Single();
                long? adjustedXp = (long?)(amount * woType.XpModifier);
                if (adjustedXp < 1)
                {
                    adjustedXp = 1;
                }

                var newMbDb = new MultiBotDb();
                var Member = new MemberController().GetMemberbyDcId(ctx.Member, ctx.Guild);

                Member.Xp = (long?)(Member.Xp + adjustedXp);

                newMbDb.Member.Update(Member);
                BankController b = new BankController();

                await b.UpdateExpense(ctx.Guild, Convert.ToInt32((amount * 1500) * woType.CreditModifier));

                newMbDb.SaveChanges();

                return isCompleted;
            } catch(Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        public void CalcXpForCompletion(WorkOrders order)
        {
            var workOrderMember = GetWorkOrderMembers(order.Id);
            foreach (var member in workOrderMember)
            {
                var acceptedMember = new MemberController().GetMemberById(member.MemberId);
                acceptedMember.Xp = (acceptedMember.Xp + 50);
                MultiBotDb.Member.Update(acceptedMember);
                MultiBotDb.SaveChanges();
            }
        }

        public async Task<WorkOrderTypes> GetWorkOrderType(DiscordChannel channel, string type)
        {
            try
            {
                switch (type.ToLower())
                {
                    case "trading":
                        return MultiBotDb.WorkOrderTypes.AsQueryable().Where(x => x.Name == "trading").SingleOrDefault();
                    case "trade":
                        return MultiBotDb.WorkOrderTypes.AsQueryable().Where(x => x.Name == "trading").SingleOrDefault();
                    case "mining":
                        return MultiBotDb.WorkOrderTypes.AsQueryable().Where(x => x.Name == "mining").SingleOrDefault();
                    case "mine":
                        return MultiBotDb.WorkOrderTypes.AsQueryable().Where(x => x.Name == "mining").SingleOrDefault();
                    case "shipping":
                        return MultiBotDb.WorkOrderTypes.AsQueryable().Where(x => x.Name == "shipping").SingleOrDefault();
                    case "ship":
                        return MultiBotDb.WorkOrderTypes.AsQueryable().Where(x => x.Name == "shipping").SingleOrDefault();
                    case "salvage":
                        return MultiBotDb.WorkOrderTypes.AsQueryable().Where(x => x.Name == "salvage").SingleOrDefault();
                    case var hand when type.ToLower().Contains("hand"):
                        return MultiBotDb.WorkOrderTypes.AsQueryable().Where(x => x.Name == "hand mineables").SingleOrDefault();
                    case var roc when type.ToLower().Contains("roc"):
                        return MultiBotDb.WorkOrderTypes.AsQueryable().Where(x => x.Name == "hand mineables").SingleOrDefault();
                    case "military":
                        return MultiBotDb.WorkOrderTypes.AsQueryable().Where(x => x.Name == "military").SingleOrDefault();

                    default:
                        await channel.SendMessageAsync("Please specify type, trading, mining, hand mining or roc mining, shipping, military, salvage");
                        throw new InvalidOperationException("Unspecified Work Order Type");

                }
            } catch(Exception e)
            {
                Console.WriteLine(e);
            }

            return null;
        }

        public List<WorkOrderRequirements> GetRequirements(int workOrderId)
        {
            return new MultiBotDb().WorkOrderRequirements.AsQueryable().Where(x => x.WorkOrderId == workOrderId && !x.isCompleted).ToList();
        }

        public WorkOrderRequirements GetRequirementById(int requirementId)
        {
            return MultiBotDb.WorkOrderRequirements.AsQueryable().Where(x => x.Id == requirementId).SingleOrDefault();
        }

        public List<WorkOrderMembers> GetWorkOrderMembers(int workOrderId)
        {
            return MultiBotDb.WorkOrderMembers.AsQueryable().Where(x => x.WorkOrderId == workOrderId).ToList();
        }

        public async Task<bool> AcceptWorkOrder(CommandContext ctx, int workOrderId)
        {
            try
            {
                var member = new MemberController().GetMemberbyDcId(ctx.Member, ctx.Guild);
                var workOrderMember = new WorkOrderMembers();
                workOrderMember.MemberId = member.UserId;
                workOrderMember.WorkOrderId = workOrderId;
                MultiBotDb.WorkOrderMembers.Add(workOrderMember);
                MultiBotDb.SaveChanges();


                await ctx.RespondAsync("The work order is yours when you've complete either part or all of the work order please use !log to log your work");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        public async Task AddWorkOrder(CommandContext ctx, string title, string description, string type, string location, List<Tuple<string, int>> reqs)
        {

            var id = new OrgController().GetOrgId(ctx.Guild);
            try
            {
                var order = new WorkOrders()
                {
                    Name = $"{title}- requested by {ctx.Member.Nickname ?? ctx.Member.DisplayName}",
                    Description = description,
                    Location = location,
                    WorkOrderTypeId = (await GetWorkOrderType(ctx.Channel, type)).Id,
                    OrgId = id,
                    isCompleted = false
                };


                MultiBotDb.WorkOrders.Add(order);
                MultiBotDb.SaveChanges();

                foreach (var item in reqs)
                {
                    var orderReqs = new WorkOrderRequirements();
                    orderReqs.Material = item.Item1;
                    orderReqs.Amount = item.Item2;
                    orderReqs.WorkOrderId = order.Id;
                    orderReqs.TypeId = order.WorkOrderTypeId;
                    MultiBotDb.WorkOrderRequirements.Add(orderReqs);
                    MultiBotDb.SaveChanges();
                }
            } catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public int GetHighestWorkOrder(int orgId)
        {
            return MultiBotDb.WorkOrders.ToList().OrderByDescending(x => x.Id).First().Id;
        }
    }
}
