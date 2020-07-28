using DSharpPlus.Entities;
using multicorp_bot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace multicorp_bot.Controllers
{
    public class FleetController
    {
        MultiBotDb MultiBotDb;
        public FleetController()
        {
            MultiBotDb = new MultiBotDb();
        }
        public DiscordEmbed GetFleetRequests(DiscordGuild guild)
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            builder.Title = "MultiCorp Fleet Requests";
            builder.Timestamp = DateTime.Now;
            builder.Description = "The below lists are ships deemed imperative by command to advance the org";
            var fleetReqs = GetOrgFleetRequests(guild);
            foreach (var req in fleetReqs)
            {
                builder.AddField(req.Name, $"Req Id: {req.Id} Total Cost: {FormatHelpers.FormattedNumber(req.TotalPrice.ToString())} \n" +
                    $"Remaining Balance: {FormatHelpers.FormattedNumber(req.RemainingPrice.ToString())}");
            }
            Random rand = new Random();
            var imgNum = rand.Next(0, fleetReqs.Count);
            builder.ImageUrl = fleetReqs[imgNum].ImgUrl;
            builder.WithFooter("Only one image can be shown. The Image is chosen randomly from the list of reqested ships");
            return builder.Build();

        }

        public List<WantedShips> GetOrgFleetRequests(DiscordGuild guild)
        {
            return MultiBotDb.WantedShips.Where(x => x.OrgId == new OrgController().GetOrgId(guild) && !x.IsCompleted).ToList();
        }

        public int CompleteFleetRequest(DiscordGuild guild)
        {
            var zeroBalanceShips = MultiBotDb.WantedShips.Where(x => x.OrgId == new OrgController().GetOrgId(guild) && !x.IsCompleted && x.RemainingPrice == 0).ToList();
            foreach(var ship in zeroBalanceShips)
            {
                ship.IsCompleted = true;
                MultiBotDb.WantedShips.Update(ship);
            }

            MultiBotDb.SaveChanges();
            int test = zeroBalanceShips.Count();
            return zeroBalanceShips.Count();
        }

        public void AddFleetRequest(string name, int price, string imgUrl, DiscordGuild guild)
        {
            var request = new WantedShips()
            {
                Id = GetHighestRequestid() + 1,
                OrgId = new OrgController().GetOrgId(guild),
                Name = name,
                TotalPrice = price,
                RemainingPrice = price,
                ImgUrl = imgUrl
            };

            MultiBotDb.WantedShips.Add(request);
            MultiBotDb.SaveChanges();
        }

        public void UpdateFleetItemAmount(int fleetId, int amount)
        {
            var req = GetFleetReqById(fleetId);
            req.RemainingPrice = req.RemainingPrice - amount;
            MultiBotDb.WantedShips.Update(req);
            MultiBotDb.SaveChanges();
        }

        public WantedShips GetFleetReqById(int fleetId)
        {
            return MultiBotDb.WantedShips.Where(x => x.Id == fleetId && !x.IsCompleted).FirstOrDefault();
        }

        private int GetHighestRequestid()
        {
            return MultiBotDb.WantedShips.ToList().OrderByDescending(x => x.Id).First().Id;
        }
    }
}
