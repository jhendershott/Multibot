using DSharpPlus.Entities;
using multicorp_bot.Helpers;
using System;
using System.Linq;

namespace multicorp_bot.Controllers
{
    public class OrgController
    {
        MultiBotDb MultiBotDb;
        TelemetryHelper tHelper = new TelemetryHelper();
        public OrgController()
        {
            MultiBotDb = new MultiBotDb();
        }


        public int AddOrg(DiscordGuild guild)
        {
            var orgContext = MultiBotDb.Orgs;
            var org = new Orgs()
            {
                OrgName = guild.Name
             };

            orgContext.Add(org);
            MultiBotDb.SaveChanges();
            return GetOrgId(guild);
        }



        public int GetOrgId(DiscordGuild guild)
        {
            var orgContext = MultiBotDb.Orgs;
            try
            {
                return orgContext.Single(x => x.OrgName == guild.Name).Id;
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                return AddOrg(guild);
            }
        }

        private int GetHighestOrgId()
        {
            return MultiBotDb.Orgs.ToList().OrderBy(x => x.Id).First().Id;
        }
    }
}
