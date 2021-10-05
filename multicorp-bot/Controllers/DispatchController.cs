using System;
using System.Collections.Generic;
using System.Linq;
using multicorp_bot.Helpers;
using DSharpPlus.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using System.Threading.Tasks;

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
            var orgD = MultiBotDb.OrgDispatch.AsQueryable().Where(x => x.OrgDispatchId == 1).ToList();

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
            DiscordMessage msg = await channels.First(x => x.Name == "bottest").SendMessageAsync("Someone Needs Medical Attention");

            return msg;
        }




    }
}
