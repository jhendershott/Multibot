using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace multicorp_bot.Controllers
{
    public class OrgController
    {
        MultiBotDb MultiBotDb;
        public OrgController()
        {
            MultiBotDb = new MultiBotDb();
        }


        public void AddOrg(DiscordGuild guild)
        {
            var orgContext = MultiBotDb.Orgs;
            var org = new Orgs()
            {
                Id = GetHighestOrgId() +1,
                OrgName = guild.Name
             };

            orgContext.Add(org);
        }



        public int GetOrgId(DiscordGuild guild)
        {
            var orgContext = MultiBotDb.Orgs;
            return orgContext.Single(x => x.OrgName == guild.Name).Id;
        }

        private int GetHighestOrgId()
        {
            return MultiBotDb.Orgs.ToList().OrderBy(x => x.Id).First().Id;
        }
    }
}
