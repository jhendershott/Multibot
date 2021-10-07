using System;
using System.Collections.Generic;
using System.Linq;
using multicorp_bot.Helpers;
using DSharpPlus.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using System.Threading.Tasks;
using multicorp_bot.Models.DbModels;

namespace multicorp_bot.Controllers
{
    public class DispatchController
    {
        MultiBotDb MultiBotDb;
        TelemetryHelper tHelper = new TelemetryHelper();

        public DispatchController()
        {
            MultiBotDb = new MultiBotDb();
        }

        public List<Orgs> GetRescueOrgs()
        {
            var orgD = MultiBotDb.OrgDispatch.AsQueryable().Where(x => x.DispatchType == 1).ToList();

            List<Orgs> orgs = new List<Orgs>();
            foreach (var org in orgD)
            {
                orgs.Add(MultiBotDb.Orgs.Single(x => x.Id == org.OrgId));
            }

            return orgs;
        }

        public async Task<DiscordMessage> SendOrgMessage(CommandContext ctx, Orgs org)
        {
            DiscordGuild guild = await ctx.Client.GetGuildAsync(ulong.Parse(org.DiscordId));
            var channels = await guild.GetChannelsAsync();
            DiscordMessage msg = await channels.First(x => x.Name == "medical-assistance").SendMessageAsync("Someone Needs Medical Attention");

            return msg;
        }

        public bool Enlist(CommandContext ctx, string type)
        {
            var org = new OrgController().GetOrgId(ctx.Guild);
            DispatchType dType = null;

            try
            {
                switch (type)
                {
                    case "medical":
                        dType = this.GetDispatchType(type);
                        var newsub = new OrgDispatch();
                        newsub.OrgId = org;
                        newsub.DispatchType = dType.DispatchTypeId;

                        MultiBotDb.OrgDispatch.Add(newsub);
                        MultiBotDb.SaveChanges();
                        break;

                    default: ctx.RespondAsync("Enlistment types include: 'medical', please try again"); break;
                }

                var en = new MultiBotDb().OrgDispatch.FirstOrDefault(x => x.OrgId == org && x.DispatchType == dType.DispatchTypeId);

                if (en != null)
                {
                    ctx.RespondAsync("You have been successfully subscribed to 'medical'");
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ctx.RespondAsync("something went wrong");
                return false;
            }

            
        }

        private DispatchType GetDispatchType(string type)
        {
            return MultiBotDb.DispatchType.First(x => x.Description == type);
        }

    }
}
