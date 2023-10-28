using DSharpPlus.Entities;
using multicorp_bot.Helpers;
using System;
using System.Linq;

namespace multicorp_bot.Controllers
{
    public class OrgController
    {
        MultiBotDb MultiBotDb;
        public OrgController()
        {
            MultiBotDb = new MultiBotDb();
        }


        public int AddOrg(DiscordGuild guild)
        {
            try
            {
                if (MultiBotDb.Orgs.Any(o => o.DiscordId == guild.Id.ToString())) return GetOrgId(guild);

                var orgContext = MultiBotDb.Orgs;
                var org = new Orgs()
                {
                    OrgName = guild.Name,
                    DiscordId = guild.Id.ToString()
                };

                orgContext.Add(org);
                MultiBotDb.SaveChanges();
            } catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return GetOrgId(guild);
        }

        public void UpdateDiscordId(DiscordGuild guild)
        {
            var orgContext = MultiBotDb.Orgs;
            try
            {
                var org = orgContext.Single(x => x.OrgName == guild.Name);
                if (org.DiscordId == null)
                {
                    org.DiscordId = guild.Id.ToString();
                    orgContext.Update(org);
                    MultiBotDb.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public int GetOrgId(DiscordGuild guild)
        {
            var orgContext = MultiBotDb.Orgs;
            try
            {
                var org = orgContext.Single(x => x.OrgName == guild.Name);
                if(org.DiscordId == null)
                {
                    org.DiscordId = guild.Id.ToString();
                    orgContext.Update(org);
                    MultiBotDb.SaveChanges();
                }
                return org.Id;
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
