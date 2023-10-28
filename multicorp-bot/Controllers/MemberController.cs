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
        public MemberController()
        {
            MultiBotDb = new MultiBotDb();
        }

        public int? AddMember(string name, int orgid, DiscordMember dcMember)
        {
            var memberContext = MultiBotDb.Member;
            var member = new Member()
            {
                OrgId = orgid,
                Username = name,
                DiscordId = dcMember.Id.ToString(),
                Xp = 0
            };

            memberContext.Add(member);
            MultiBotDb.SaveChanges();

            return GetMemberId(name, orgid, dcMember);
        }

        public Member GetMemberById(int id)
        {
            return MultiBotDb.Member.Single(x => x.UserId == id);
        }

        public int? GetMemberId(string name, int orgId, DiscordMember member)
        {
            var memberContext = MultiBotDb.Member;
            try
            {
                return memberContext.Single(x => x.Username == name && x.OrgId == orgId).UserId;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return AddMember(name, orgId, member);
            }
        }

        public Member GetMember(string name, DiscordGuild guild)
        {
            var orgId = new OrgController().GetOrgId(guild);
            var memberCtx = MultiBotDb.Member;
            return memberCtx.SingleOrDefault(x => x.Username == name && x.OrgId == orgId);
        }

        public Member GetMemberbyDcId(DiscordMember member, DiscordGuild guild)
        {
            var orgId = new OrgController().GetOrgId(guild);
            var memberCtx = MultiBotDb.Member;
            var mem = memberCtx.AsQueryable().Where(x => x.DiscordId == member.Id.ToString() && x.OrgId == orgId).FirstOrDefault();
            if (mem != null)
            {
                return mem;
            }
            else
            {
                if (member.Nickname != null)
                {
                    AddMember(member.Nickname, orgId, member);
                }
                else
                {
                    AddMember(member.DisplayName, orgId, member);
                }
                return memberCtx.Single(x => x.DiscordId == member.Id.ToString() && x.OrgId == orgId);
            }
        }

        public async Task<Member> GetMemberbyDcId(DiscordUser member, DiscordGuild guild)
        {
            var orgId = new OrgController().GetOrgId(guild);
            var memberCtx = MultiBotDb.Member;
            if (memberCtx.Any(x => x.DiscordId == member.Id.ToString() && x.OrgId == orgId))
            {
                return memberCtx.Single(x => x.DiscordId == member.Id.ToString() && x.OrgId == orgId);
            }
            else
            {
                DiscordMember dcMem = await guild.GetMemberAsync(member.Id);
                if (dcMem.Nickname != null)
                {
                    AddMember(dcMem.Nickname, orgId, dcMem);
                }
                else
                {
                    AddMember(dcMem.DisplayName, orgId, dcMem);
                }
                return memberCtx.Single(x => x.DiscordId == member.Id.ToString() && x.OrgId == orgId);
            }
        }

        public async Task<DiscordMember> GetDiscordMemberByMemberId(DiscordGuild guild, int memberId)
        {
            string discordId = MultiBotDb.Member.Single(x => x.OrgId == new OrgController().GetOrgId(guild) && x.UserId == memberId).DiscordId;
            return await guild.GetMemberAsync(ulong.Parse(discordId));
        }

        public void UpdateMemberName(CommandContext ctx, string oldName, string newName, DiscordGuild guild)
        {
            var member = GetMember(oldName, guild);
            var memberCtx = MultiBotDb.Member;
            if (member == null)
            {
                memberCtx.Add(new Member
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

        public List<Member> GetMembersByOrgId(int orgId)
        {
            var memberContext = MultiBotDb.Member;
            return memberContext.AsQueryable().Where(x => x.OrgId == orgId).ToList();
        }

        public long? UpdateExperiencePoints(string workOrderType, BankTransaction trans)
        {
            Member member = GetMemberbyDcId(trans.Member, trans.Guild);
            double xpMod = new WorkOrderController().GetExpModifier(workOrderType);
            switch (workOrderType)
            {
                case "merits":
                    member.Xp = member.Xp + Convert.ToInt64(trans.Merits * xpMod);
                    break;
                case "credits":
                    member.Xp = member.Xp + Convert.ToInt64(trans.Amount * xpMod);
                    break;

                default:
                    Console.WriteLine("Currently support xp modifiers are merit and credits");
                    break;
            }

            member.Xp = member.Xp + Convert.ToInt64(trans.Amount * xpMod);
            MultiBotDb.Member.Update(member);
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
            var memberByXP = MultiBotDb.Member.Where(x => x.OrgId == orgId).OrderBy(x => x.Xp).ToList();

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            builder.Title = $"{guild.Name} Top XP Earners";

            for (int i = 0; i < 10; i++)
            {
                builder.AddField(memberByXP[i].Username, $"Current Experience Points: {memberByXP[i].Xp}");
            }

            builder.WithFooter("You can gain experience by completing dispatches, recruiting, participating in commerce events or depositing credits or merits to the bank");

            return builder.Build();

        }
    }
}
