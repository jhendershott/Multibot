using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
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
            return memberCtx.Single(x => x.Username == name && x.OrgId == orgId);
        }

        public Mcmember GetMemberbyDcId(DiscordMember member, DiscordGuild guild)
        {
            var orgId = new OrgController().GetOrgId(guild);
            var memberCtx = MultiBotDb.Mcmember;
            if(memberCtx.Any(x => x.DiscordId == member.Id.ToString()))
            {
                return memberCtx.Single(x => x.DiscordId == member.Id.ToString() && x.OrgId == orgId);
            }
            else
            {
                AddMember(new Ranks().GetNickWithoutRank(member.Nickname), orgId, member);
                return memberCtx.Single(x => x.DiscordId == member.Id.ToString() && x.OrgId == orgId);
            }
            
            
        }

        public async Task<DiscordMember> GetDiscordMemberByMemberId(CommandContext ctx, int id)
        {
            return await ctx.Guild.GetMemberAsync(ulong.Parse(GetMemberById(id).DiscordId));
        }

        public void UpdateMemberName(string oldName, string newName, DiscordGuild guild)
        {
            var member = GetMember(oldName, guild);
            var memberCtx = MultiBotDb.Mcmember;
            member.Username = newName;
            memberCtx.Update(member);
            MultiBotDb.SaveChanges();
            
        }

        public List<Mcmember> GetMembersByOrgId(int orgId)
        {
            var memberContext = MultiBotDb.Mcmember;
            return memberContext.Where(x => x.OrgId == orgId).ToList();
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
                case "credit":
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
    }
}
