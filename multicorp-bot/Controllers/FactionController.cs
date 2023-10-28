using System.Linq;
using DSharpPlus.Entities;
using multicorp_bot.Controllers;
using multicorp_bot.Models;
namespace multicorp_bot
{
    public class FactionController
    {
        MultiBotDb MultiBotDb;

        public FactionController()
        {
            MultiBotDb = new MultiBotDb();
        }

        public string AddFaction(string name, DiscordGuild guild)
        {
            MultiBotDb.Factions.Add(
                new Factions()
                    {
                        Name = name,
                    }
                );
            MultiBotDb.SaveChanges();
            return GetFactionIdByName(name).ToString();
        }

        public void AddFactionFavor(int factionId, int orgId, int favorPoint = 0)
        {
            MultiBotDb.FactionFavors.Add(
                new FactionFavor()
                {
                    FactionID = factionId,
                    OrgId = orgId,
                    FavorPoints = favorPoint
                }
                );
            MultiBotDb.SaveChanges();
        }

        public string GetFactionById(int id) {
            return MultiBotDb.Factions.SingleOrDefault(x => x.FactionId == id).Name;
        }

        public int GetFactionIdByName(string name)
        {
            return MultiBotDb.Factions.SingleOrDefault(x => x.Name.ToLower() == name.ToLower()).FactionId;
        }

        public void AddFactionFavor(DiscordGuild guild, int factionId)
        {
            int orgId = new OrgController().GetOrgId(guild);
            var factionFavor = MultiBotDb.FactionFavors.AsQueryable().Where(x => x.OrgId == orgId && x.FactionID == factionId);
            if (factionFavor.Count() != 0)
            {
                factionFavor.First().FavorPoints++;
                MultiBotDb.SaveChanges();
            }
            else
            {
                AddFactionFavor(factionId, orgId, 1);
            }
        }

    }
}

