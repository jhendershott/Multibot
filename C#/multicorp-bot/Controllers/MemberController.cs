using DSharpPlus.Entities;
using multicorp_bot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace multicorp_bot.Controllers
{
    public class MemberController
    {
        MultiBotDb MultiBotDb;
        public MemberController() {
            MultiBotDb = new MultiBotDb();
        }

        public int? AddMember(string name, int orgid)
        {
            var memberContext = MultiBotDb.Mcmember;
            var member = new Mcmember()
            {
                OrgId = orgid,
                Username = name,
                UserId = GetHighestUserId() + 1,
                Xp = 0
            };

            memberContext.Add(member);

            return GetMemberId(name, orgid);
        }

        public int? GetMemberId(string name, int orgId)
        {
            var memberContext = MultiBotDb.Mcmember;
            try
            {
                return memberContext.Single(x => x.Username == name && x.OrgId == orgId).UserId;
            }
            catch
            {
                return null;
            }
        }

        public Mcmember GetMember(string name, DiscordGuild guild)
        {
            var orgId = new OrgController().GetOrgId(guild);
            var memberCtx = MultiBotDb.Mcmember;
            return memberCtx.Single(x => x.Username == name && x.OrgId == orgId);
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
            return MultiBotDb.Mcmember.ToList().OrderBy(x => x.UserId).First().UserId;
        }
    }
}
