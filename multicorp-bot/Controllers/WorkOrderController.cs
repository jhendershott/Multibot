using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore.Query.Internal;
using multicorp_bot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace multicorp_bot.Controllers
{
    public class WorkOrderController
    {
        MultiBotDb MultiBotDb;
        TelemetryHelper tHelper = new TelemetryHelper();
        public WorkOrderController()
        {
            MultiBotDb = new MultiBotDb();
        }

        public double GetExpModifier(string modName)
        {
            return MultiBotDb.WorkOrderTypes.Where(x => x.Name == modName).FirstOrDefault().XpModifier;
        }

        public async Task<Tuple<DiscordEmbed, WorkOrders>> GetWorkOrders(CommandContext ctx, string workOrderType)
        {
            try
            {
                WorkOrders order = null;
                var orderType = await GetWorkOrderType(ctx, workOrderType);
                var wOrders = MultiBotDb.WorkOrders.Where(x => x.OrgId == new OrgController().GetOrgId(ctx.Guild) && x.WorkOrderTypeId == orderType.Id && !x.isCompleted).ToList();

                DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
                var type = FormatHelpers.Capitalize(orderType.Name);

                builder.Title = $"{ctx.Guild.Name} {type} Dispatch";
                builder.Description = $"There are {wOrders.Count} {type} Work Orders, here's one that may interest you?";
                builder.Timestamp = DateTime.Now;

                if (wOrders.Count > 0)
                {
                    Random rand = new Random();
                    int randOrder = rand.Next(0, wOrders.Count);
                    order = wOrders[randOrder];

                    var workOrderMember = GetWorkOrderMembers(order.Id);
                    StringBuilder membersStr = new StringBuilder();

                    builder.AddField("Location", order.Location);
                    StringBuilder reqString = new StringBuilder();
                    foreach (WorkOrderRequirements req in GetRequirements(order.Id))
                    {
                        reqString.Append($"\u200b\nRequirement ID: {req.Id}\n");
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

                    builder.WithFooter("If you would like to accept this dispatch please respond with ✅" +
                        "\n to decline and see another use X" +
                        "\n If you are not interested in a dispatch at this time simply do nothing at all and the request will time out");

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
                tHelper.LogException($"Method: GetWorkOrders; Org: {ctx.Guild.Name}; Message: {ctx.Message}; User:{ctx.Member.Nickname}", e);
                Console.WriteLine(e);
                return null;
            }
        }

        public async Task<DiscordEmbed> GetWorkOrderByMember(CommandContext ctx)
        {
            try
            {
                Mcmember mem = new MemberController().GetMemberbyDcId(ctx.Member, ctx.Guild);
                List<WorkOrderMembers> memberOrders = MultiBotDb.WorkOrderMembers.Where(x => x.MemberId == mem.UserId).ToList();
                List<WorkOrders> wOrders = new List<WorkOrders>();

                foreach(var o in memberOrders)
                {
                    wOrders.Add(MultiBotDb.WorkOrders.Where(x => x.Id == o.WorkOrderId).FirstOrDefault());
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
                tHelper.LogException($"Method: GetWorkOrders; Org: {ctx.Guild.Name}; Message: {ctx.Message}; User:{ctx.Member.Nickname}", e);
                Console.WriteLine(e);
                return null;
            }
        }

        public void LogWork(CommandContext ctx, int id, string type, int amount)
        {
            bool isCompleted = true;
            var orderReqs = MultiBotDb.WorkOrderRequirements.Where(x => x.WorkOrderId == id).ToList();
            var orderReq = orderReqs.Where(x => x.Material.ToLower() == type.ToLower()).SingleOrDefault();

            var order = MultiBotDb.WorkOrders.Where(x => x.Id == id).SingleOrDefault();
            if (order.OrgId != new OrgController().GetOrgId(ctx.Guild) || order.isCompleted)
            {
                ctx.RespondAsync("Please try again with a valid Work Order Id");
                return;
            }

            orderReq.Amount = orderReq.Amount - amount;
            if (orderReq.Amount <= 0)
            {
                orderReq.isCompleted = true;
                ctx.RespondAsync($"Great job you have fulfilled work order for {type}");
            }
            else
            {
                ctx.RespondAsync($"Work Order amount remaining: {orderReq.Amount} SCU of {type}");
            }

            foreach(WorkOrderRequirements item in orderReqs)
            {
                if (!item.isCompleted)
                    isCompleted = false;
            }

            MultiBotDb.WorkOrderRequirements.Update(orderReq);

            if (isCompleted)
            {
                order.isCompleted = true;
                MultiBotDb.WorkOrders.Update(order);
                ctx.RespondAsync($"Great job you have completed the Work Order {type}");
            }

            var Member = new MemberController().GetMemberbyDcId(ctx.Member, ctx.Guild);
            Member.Xp = (long?)(Member.Xp + (amount * MultiBotDb.WorkOrderTypes.Where(x => x.Id == orderReq.TypeId).Single().XpModifier));

            MultiBotDb.Mcmember.Update(Member);
            MultiBotDb.SaveChanges();
        }

        public async Task<WorkOrderTypes> GetWorkOrderType(CommandContext ctx, string type)
        {
            try
            {
                switch (type.ToLower())
                {
                    case "trading":
                        return MultiBotDb.WorkOrderTypes.Where(x => x.Name == "trading").SingleOrDefault();
                    case "trade":
                        return MultiBotDb.WorkOrderTypes.Where(x => x.Name == "trading").SingleOrDefault();
                    case "mining":
                        return MultiBotDb.WorkOrderTypes.Where(x => x.Name == "mining").SingleOrDefault();
                    case "mine":
                        return MultiBotDb.WorkOrderTypes.Where(x => x.Name == "mining").SingleOrDefault();
                    case "shipping":
                        return MultiBotDb.WorkOrderTypes.Where(x => x.Name == "shipping").SingleOrDefault();
                    case "ship":
                        return MultiBotDb.WorkOrderTypes.Where(x => x.Name == "shipping").SingleOrDefault();
                    case var hand when type.ToLower().Contains("hand"):
                        return MultiBotDb.WorkOrderTypes.Where(x => x.Name == "Hand Mineables").SingleOrDefault();
                    case var roc when type.ToLower().Contains("roc"):
                        return MultiBotDb.WorkOrderTypes.Where(x => x.Name == "Hand Mineables").SingleOrDefault();

                    default:
                        await ctx.RespondAsync("Please specify type, trading, mining, hand mining or roc mining, or shipping");
                        throw new InvalidOperationException("Unspecified Work Order Type");

                }
            } catch(Exception e)
            {
                tHelper.LogException($"Method: GetWorkOrderType; Org: {ctx.Guild.Name}; Message: {ctx.Message}; User:{ctx.Member.Nickname}", e);
                Console.WriteLine(e);
            }

            return null;
        }

        public List<WorkOrderRequirements> GetRequirements(int workOrderId)
        {
            return MultiBotDb.WorkOrderRequirements.Where(x => x.WorkOrderId == workOrderId && !x.isCompleted).ToList();
        }

        public WorkOrderRequirements GetRequirementById(int requirementId)
        {
            return MultiBotDb.WorkOrderRequirements.Where(x => x.Id == requirementId).SingleOrDefault();
        }

        public List<WorkOrderMembers> GetWorkOrderMembers(int workOrderId)
        {
            return MultiBotDb.WorkOrderMembers.Where(x => x.WorkOrderId == workOrderId).ToList();
        }

        public bool AcceptWorkOrder(CommandContext ctx, int workOrderId)
        {
            try
            {
                var member = new MemberController().GetMemberbyDcId(ctx.Member, ctx.Guild);
                var workOrderMember = new WorkOrderMembers();
                workOrderMember.MemberId = member.UserId;
                workOrderMember.WorkOrderId = workOrderId;
                MultiBotDb.WorkOrderMembers.Add(workOrderMember);
                MultiBotDb.SaveChanges();
                return true;
            }
            catch (Exception e)
            {
                tHelper.LogException($"Method: AcceptWorkOrder; Org: {ctx.Guild.Name}; Message: {ctx.Message}; User:{ctx.Member.Nickname}", e);
                Console.WriteLine(e);
                return false;
            }
        }

        public async Task AddWorkOrder(CommandContext ctx, string name, string description, string type, string location, List<Tuple<string, int>> reqs)
        {

            var id = new OrgController().GetOrgId(ctx.Guild);
            try
            {
                var order = new WorkOrders()
                {
                    Id = GetHighestWorkOrder(id) + 1,
                    Name = name,
                    Description = description,
                    Location = location,
                    WorkOrderTypeId = (await GetWorkOrderType(ctx, type)).Id,
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
                tHelper.LogException($"Method: AddWorkOrder; Org: {ctx.Guild.Name}; Message: {ctx.Message}; User:{ctx.Member.Nickname}", e);
                Console.WriteLine(e);
            }
        }

        public int GetHighestWorkOrder(int orgId)
        {
            return MultiBotDb.WorkOrders.ToList().OrderByDescending(x => x.Id).First().Id;
        }
    }
}
