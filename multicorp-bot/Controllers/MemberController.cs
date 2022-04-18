using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Net.Models;
using multicorp_bot.Helpers;
using multicorp_bot.Models;
using multicorp_bot.POCO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace multicorp_bot.Controllers
{
    public class MemberController
    {
        MultiBotDb MultiBotDb;
        TelemetryHelper tHelper = new TelemetryHelper();
        public MemberController() {
            MultiBotDb = new MultiBotDb();
        }

        public int? AddMember(string name, int orgid, DiscordMember dcMember)
        {
            var memberContext = MultiBotDb.Mcmember;
            var test = GetHighestUserId() + 1;
            var member = new Mcmember()
            {
                OrgId = orgid,
                Username = name,
                UserId = GetHighestUserId() + 1,
                DiscordId = dcMember.Id.ToString(),
                Xp = 0
            };

            memberContext.Add(member);
            MultiBotDb.SaveChanges();

            return GetMemberId(name, orgid, dcMember);
        }

        public Mcmember GetMemberById(int id)
        {
            return MultiBotDb.Mcmember.Single(x => x.UserId == id);
        }

        public int? GetMemberId(string name, int orgId, DiscordMember member)
        {
            var memberContext = MultiBotDb.Mcmember;
            try
            {
                return memberContext.Single(x => x.Username == name && x.OrgId == orgId).UserId;
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                return AddMember(name, orgId, member);
            }
        }

        public Mcmember GetMember(string name, DiscordGuild guild)
        {
            var orgId = new OrgController().GetOrgId(guild);
            var memberCtx = MultiBotDb.Mcmember;
            return memberCtx.SingleOrDefault(x => x.Username == name && x.OrgId == orgId);
        }

        public Mcmember GetMemberbyDcId(DiscordMember member, DiscordGuild guild)
        {
            var orgId = new OrgController().GetOrgId(guild);
            var memberCtx = MultiBotDb.Mcmember;
            if(memberCtx.Any(x => x.DiscordId == member.Id.ToString() && x.OrgId == orgId ))
            {
                return memberCtx.Single(x => x.DiscordId == member.Id.ToString() && x.OrgId == orgId);
            }
            else
            {
                AddMember(member.Nickname, orgId, member);
                return memberCtx.Single(x => x.DiscordId == member.Id.ToString() && x.OrgId == orgId);
            }
        }

        public async Task<DiscordMember> GetDiscordMemberByMemberId(CommandContext ctx, int id)
        {
            return await ctx.Guild.GetMemberAsync(ulong.Parse(GetMemberById(id).DiscordId));
        }

        public void UpdateMemberName(CommandContext ctx, string oldName, string newName, DiscordGuild guild)
        {
            var member = GetMember(oldName, guild);
            var memberCtx = MultiBotDb.Mcmember;
            if (member == null)
            {
                memberCtx.Add(new Mcmember
                {
                    Username = newName,
                    DiscordId = ctx.Member.Id.ToString(),
                    OrgId = new OrgController().GetOrgId(guild),
                    Xp = 0,
                });
            }
            else
            {
                member.Username = newName;
                memberCtx.Update(member);

            }

            MultiBotDb.SaveChanges();
        }

        public List<Mcmember> GetMembersByOrgId(int orgId)
        {
            var memberContext = MultiBotDb.Mcmember;
            return memberContext.AsQueryable().Where(x => x.OrgId == orgId).ToList();
        }

        private int GetHighestUserId()
        {
            return MultiBotDb.Mcmember.ToList().OrderByDescending(x => x.UserId).First().UserId;
        }

        public long? UpdateExperiencePoints(string workOrderType, BankTransaction trans)
        {
            Mcmember member = GetMemberbyDcId(trans.Member, trans.Guild);
            double xpMod = new WorkOrderController().GetExpModifier(workOrderType);
            switch (workOrderType)
            {
                case "merits":
                    member.Xp = member.Xp + Convert.ToInt64(trans.Merits * xpMod);
                    break;
                case "credits":
                    member.Xp = member.Xp + Convert.ToInt64(trans.Amount * xpMod);
                    break;

                default: Console.WriteLine("Currently support xp modifiers are merit and credits");
                    break;
            }

            member.Xp = member.Xp + Convert.ToInt64(trans.Amount * xpMod);
            MultiBotDb.Mcmember.Update(member);
            MultiBotDb.SaveChanges();

            return member.Xp;
        }

        public string GetMemberXP(DiscordGuild org, DiscordMember mem)
        {
            var member = this.GetMemberbyDcId(mem, org);
            int amountToNextRank = 0;


            return $"Your Current Xp: {member.Xp} \n you're {amountToNextRank} away from reaching the minimum XP for your next rank";
        }

        public DiscordEmbed GetTopXp(DiscordGuild guild)
        {
            var orgId = new OrgController().GetOrgId(guild);
            var memberByXP = MultiBotDb.Mcmember.Where(x => x.OrgId == orgId).OrderBy(x => x.Xp).ToList();

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            builder.Title = $"{guild.Name} Top XP Earners";

            for(int i = 0; i < 10; i++)
            {
                builder.AddField(memberByXP[i].Username, $"Current Experience Points: {memberByXP[i].Xp}");
            }

            builder.WithFooter("You can gain experience by completing dispatches, recruiting, participating in commerce events or depositing credits or merits to the bank");

            return builder.Build();

        }

        public async Task<bool> StripRank(DiscordMember member)
        {
            try
            {
                Ranks rank = new Ranks();
                var newnick = rank.GetNickWithoutRank(member);
                MultiBotDb db = new MultiBotDb();
                var record = new OrgRankStrip()
                {
                    DiscordId = member.Id.ToString(),
                    OldNick = member.DisplayName,
                    NewNick = newnick,
                    OrgName = member.Guild.Name
                };

                db.OrgRankStrip.Add(record);
                await db.SaveChangesAsync();
                
                var rec = db.OrgRankStrip.SingleOrDefault(x => x.DiscordId == member.Id.ToString());
                await member.ModifyAsync(x => x.Nickname = $"MC{rec.Id} {newnick}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }

            return true;
        }

        public async Task<bool> RestoreRanks(CommandContext ctx)
        {
            try
            {
                Ranks rank = new Ranks();
                
                MultiBotDb db = new MultiBotDb();

                var usersInOrg = db.OrgRankStrip.Where(x => x.OrgName == ctx.Guild.Name);
                var list = usersInOrg.ToList();

  
                foreach (var user in usersInOrg)
                {
                    try
                    {
                        var member = await ctx.Guild.GetMemberAsync(ulong.Parse(user.DiscordId));
                        await member.ModifyAsync(x => x.Nickname = user.OldNick);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                foreach (var record in list)
                {
                    db.OrgRankStrip.Remove(record);
                }
                await db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return true;
        }
    }
}
