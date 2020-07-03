using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace multicorp_bot
{
    public class Ranks
    {

        public List<Rank> MilRanks { get; set; } = new List<Rank>() {
            new Rank () { RankName = "Recruit", Abbreviation = "RCT", Number = 0 },
            new Rank () { RankName = "Cadet", Abbreviation = "CDT", Number = 1 },
            new Rank () { RankName = "Private", Abbreviation = "PVT", Number = 2 },
            new Rank () { RankName = "Private Second Class", Abbreviation = "PV2", Number = 3 },
            new Rank () { RankName = "Private First Class", Abbreviation = "PFC", Number = 4 },
            new Rank () { RankName = "Specialist", Abbreviation = "SPC", Number = 5 },
            new Rank () { RankName = "Lance Corporal", Abbreviation = "LCPL", Number = 6 },
            new Rank () { RankName = "Corporal", Abbreviation = "CPL", Number = 7 },
            new Rank () { RankName = "Sergeant", Abbreviation = "SGT", Number = 8 },
            new Rank () { RankName = "Staff Sergeant", Abbreviation = "SSG", Number = 9 },
            new Rank () { RankName = "Second Lieutenant", Abbreviation = "2LT", Number = 10 },
            new Rank () { RankName = "Lieutenant", Abbreviation = "LT", Number = 11 },
            new Rank () { RankName = "Lieutenant Commander", Abbreviation = "LCDR", Number = 12 },
            new Rank () { RankName = "Commander", Abbreviation = "CMDR", Number = 13 },
            new Rank () { RankName = "Captain", Abbreviation = "CPT", Number = 14 },
            new Rank () { RankName = "Vice Admiral", Abbreviation = "VADM", Number = 15 },
            new Rank () { RankName = "Admiral", Abbreviation = "ADM", Number = 16 },
            new Rank () { RankName = "High Admiral", Abbreviation = "HADM", Number = 17 },
        };

        //Update the abbreviations for every member
        public void UpdateAllRanks(DiscordGuild guild)
        {
            foreach (var member in guild.Members)
            {
                var rank = GetMatchingRank(member);

                if (rank != null)
                    member.ModifyAsync($"[{rank.Abbreviation}] {member.Username}");
            }

        }

        //Promote a member
        public async Task<bool> Promote(DiscordMember member)
        {
            try
            {
                var currentRank = GetMatchingRank(member);
                var newRank = MilRanks.Where(x => x.Number == currentRank.Number + 1).FirstOrDefault();
                await member.GrantRoleAsync(member.Guild.Roles.Where(x => x.Name == newRank.RankName).FirstOrDefault());
                await member.RevokeRoleAsync(member.Guild.Roles.Where(x => x.Name == currentRank.RankName).FirstOrDefault());
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        //Demote a member
        public async Task<bool> Demote(DiscordMember member)
        {
            try
            {
                var currentRank = GetMatchingRank(member);
                var newRank = MilRanks.Where(x => x.Number == currentRank.Number - 1).FirstOrDefault();
                await member.RevokeRoleAsync(member.Guild.Roles.Where(x => x.Name == currentRank.RankName).FirstOrDefault());
                await member.GrantRoleAsync(member.Guild.Roles.Where(x => x.Name == newRank.RankName).FirstOrDefault());
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        //Assignst the starter rank
        public async Task<bool> Recruit(DiscordMember member)
        {
            try
            {
                await member.GrantRoleAsync(member.Guild.Roles.Where(x => x.Name == MilRanks.OrderBy(y => y.Number).First().RankName).FirstOrDefault());
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public Rank GetMatchingRank(DiscordMember member)
        {
            var roleNamesForMember = member.Roles.Select(y => y.Name);
            var matchingRole = MilRanks.Where(x => roleNamesForMember.Contains(x.RankName)).FirstOrDefault();

            return matchingRole;
        }
    }
}