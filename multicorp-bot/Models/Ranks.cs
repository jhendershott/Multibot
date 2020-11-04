using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Net.Models;
using multicorp_bot.Helpers;

namespace multicorp_bot
{
    public class Ranks
    {
        TelemetryHelper tHelper = new TelemetryHelper();

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
            new Rank () { RankName = "Captain", Abbreviation = "CAPT", Number = 14 },
            new Rank () { RankName = "Vice Admiral", Abbreviation = "VADM", Number = 15 },
            new Rank () { RankName = "Admiral", Abbreviation = "ADM", Number = 16 },
            new Rank () { RankName = "High Admiral", Abbreviation = "HADM", Number = 17 },
        };

        public List<Rank> CommerceRanks { get; set; } = new List<Rank>()
        {
            new Rank () { RankName = "Freelancer I", Abbreviation = "FLNCR", Number = 20 },
            new Rank () { RankName = "Freelancer II", Abbreviation = "2FLNCR" , Number = 21 },
            new Rank () { RankName = "Freelancer III", Abbreviation = "3FLNCR", Number = 22 },
            new Rank () { RankName = "Junior Associate", Abbreviation = "JASSC", Number = 23 },
            new Rank () { RankName = "Associate I", Abbreviation = "ASSC", Number = 24 },
            new Rank () { RankName = "Associate II", Abbreviation = "2ASSC", Number = 25 },
            new Rank () { RankName = "Associate III", Abbreviation = "3SSC", Number = 26 },
            new Rank () { RankName = "Senior Associate", Abbreviation = "SASSC", Number = 27 },
            new Rank () { RankName = "Manager", Abbreviation = "MGR", Number = 28 },
            new Rank () { RankName = "Senior Manager", Abbreviation = "SMGR", Number = 29 },
            new Rank () { RankName = "Staff Manager", Abbreviation = "STMGR", Number = 30 },
            new Rank () { RankName = "Supervisor", Abbreviation = "SUP", Number = 31 },
            new Rank () { RankName = "Director", Abbreviation = "DIR", Number = 32 },
            new Rank () { RankName = "Senior Director", Abbreviation = "SDIR", Number = 33 },
            new Rank () { RankName = "Department Admin", Abbreviation = "ADMIN", Number = 34 },
            new Rank () { RankName = "Assistant VP", Abbreviation = "AVP", Number = 35 },
            new Rank () { RankName = "Department VP", Abbreviation = "VP", Number = 36 },
            new Rank () { RankName = "Chief Operation Officer", Abbreviation = "COO", Number = 37 },
            new Rank () { RankName = "Chief Financial Officer", Abbreviation = "CFO", Number = 38 },
            new Rank () { RankName = "CEO", Abbreviation = "CEO", Number = 39 }
        };

        //Update the abbreviations for every member
        public void UpdateAllRanks(DiscordGuild guild)
        {
            foreach (var member in guild.Members)
            {
                var rank = GetMatchingRank(member.Value);

                if (rank != null)
                    member.Value.ModifyAsync(x => x.Nickname = $"[{rank.Abbreviation}] {member.Value.Username}");
                
            }

        }

        //Promote a member
        public async Task<bool> Promote(DiscordMember member)
        {
            try
            {
                var currentRank = GetMatchingRank(member);
                var newRank = MilRanks.Where(x => x.Number == currentRank.Number + 1).FirstOrDefault();
                if(newRank == null)
                {
                    newRank = CommerceRanks.Where(x => x.Number == currentRank.Number + 1).FirstOrDefault();
                }

                await member.GrantRoleAsync(member.Guild.Roles.Where(x => x.Value.Name == newRank.RankName).FirstOrDefault().Value);
                await member.RevokeRoleAsync(member.Guild.Roles.Where(x => x.Value.Name == currentRank.RankName).FirstOrDefault().Value);
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
                if (newRank == null)
                {
                    newRank = CommerceRanks.Where(x => x.Number == currentRank.Number - 1).FirstOrDefault();
                }

                await member.RevokeRoleAsync(member.Guild.Roles.Where(x => x.Value.Name == currentRank.RankName).FirstOrDefault().Value);
                await member.GrantRoleAsync(member.Guild.Roles.Where(x => x.Value.Name == newRank.RankName).FirstOrDefault().Value);
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
                await member.GrantRoleAsync(member.Guild.Roles.Where(x => x.Value.Name == MilRanks.OrderBy(y => y.Number).First().RankName).FirstOrDefault().Value);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public Rank GetMatchingRank(DiscordMember member)
        {
            List<string> roleNamesForMember = new List<string>();

            foreach (var role in member.Roles)
            {
                roleNamesForMember.Add(role.Name); 
            }
            //var roleNamesForMember = member.Roles.ToList();

            var roles = MilRanks.Where(x => roleNamesForMember.Contains(x.RankName)).ToList();

            var matchingRole = MilRanks.Where(x => roleNamesForMember.Contains(x.RankName)).FirstOrDefault();
            if(matchingRole == null)
            {
                matchingRole = CommerceRanks.Where(x => roleNamesForMember.Contains(x.RankName)).FirstOrDefault();
            }
            
            return matchingRole;
        }

        public string GetNickWithoutRank(DiscordMember member)
        {
            string nick = member.Nickname;
            var rank = MilRanks.Find(x => x.Abbreviation == nick.Split(" ")[0].Replace(".", ""));
            var commRank = MilRanks.Find(x => x.Abbreviation == nick.Split(" ")[0].Replace(".", ""));
            if (rank == null && commRank == null)
            {
                return nick;
            }
            else
            {
                return nick.Split(" ")[1];
            }
        }

        public string GetNickWithoutRank(string memberName)
        {
            try
            {
                var rank = MilRanks.Find(x => x.Abbreviation == memberName.Split(" ")[0].Replace(".", ""));
                var commRank = MilRanks.Find(x => x.Abbreviation == memberName.Split(" ")[0].Replace(".", ""));
                if (rank == null && commRank == null)
                {
                    return memberName;
                }
                else
                {
                    return memberName.Split(" ")[1];
                }
            }
            catch
            {
                return memberName;
            }
        }


        public string GetUpdatedNickname(DiscordMember member, int advancement = 1)
        {
            string nick = member.Nickname;
            var rank = MilRanks.Find(x => x.Abbreviation == nick.Split(" ")[0].Replace(".", ""));
   
            var commRank = CommerceRanks.Find(x => x.Abbreviation == nick.Split(" ")[0].Replace(".", ""));
            if (rank == null && commRank == null)
            {
                rank = GetMatchingRank(member);
                return $"{rank.Abbreviation}. {nick}";
            }
            else if(rank == null)
            {
                return nick.Replace(commRank.Abbreviation, CommerceRanks.Find(x => x.Number == commRank.Number + advancement).Abbreviation);
            }
            else
            {
                return nick.Replace(rank.Abbreviation, MilRanks.Find(x => x.Number == rank.Number + advancement).Abbreviation);
            }
        }

        public string GetUpdatedNickname(DiscordMember member, string nickname)
        {
            string nick = nickname;
            var rank = GetMatchingRank(member);
     
            return $"{rank.Abbreviation}. {nick}";
        }
    }
}