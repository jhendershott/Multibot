require("dotenv").config()
const Discord = require("discord.js")
const client = new Discord.Client()

client.login(process.env.BotToken)
client.on("ready", () => {
  console.log(`Logged in as ${client.user.tag}!`)
})

var ranks = ['RCT', 'CDT', 'PVT', 'PV2', 'PFC', 'SPC', 'LCPL', 'CPL', 'SGT', 'SSG', 'LT2', 'LT', 'LCDR', 'CDR', 'CAPT', 'VADM', 'ADM', 'HADM']

var rankAbbrevs = [
  {rank: 'Recruit', abbrev: 'RCT', num: 0 },
  {rank: 'Cadet', abbrev: 'CDT', num: 1  },
  {rank: 'Private', abbrev: 'PVT', num: 2  },
  {rank: 'Private Second Class', abbrev: 'PV2', num: 3 },
  {rank: 'Private First Class', abbrev: 'PFC', num: 4 },
  {rank: 'Specialist', abbrev: 'SPC', num: 5 },
  {rank: 'Lance Corporal', abbrev: 'LCPL', num: 6 },
  {rank: 'Corporal', abbrev: 'CPL', num: 7 },
  {rank: 'Sergeant', abbrev: 'SGT' , num: 8 },
  {rank: 'Staff Sergeant', abbrev: 'SSG' , num: 9 },
  {rank: 'Second Lieutenant', abbrev: 'LT2' , num: 10 },
  {rank: 'Lieutenant', abbrev: 'LT' , num: 11 },
  {rank: 'Lieutenant Commander', abbrev: 'LCDR' , num: 12 },
  {rank: 'Commander', abbrev: 'CDR' , num: 13 },
  {rank: 'Captain', abbrev: 'CPT' , num: 14 },
  {rank: 'Vice Admiral', abbrev: 'VADM' , num: 15 },
  {rank: 'Admiral', abbrev: 'ADM' , num: 16 },
  {rank: 'High Admiral', abbrev: 'HADM' , num: 17 },
]

client.on("message", msg => {
    try{
      if (msg.content.startsWith("!UpdateRank")) {
        if(msg.member.hasPermission('MANAGE_ROLES')){
        var newRank = msg.content.split(/ +/);;
        console.log(newRank);
        var updateFrom
        var updateTo 
        var rankFrom
        var rankTo
        if(newRank[2].includes('>')){
          var rankUpdates = newRank[2].split('>'); 
          updateFrom = rankUpdates[0];
          updateTo = rankUpdates[1];   
          rankFrom = msg.guild.roles.cache.find(role => role.name === GetRank(updateFrom).rank);
        }
        else {
          updateTo = newRank[2];
        }

        if(ranks.includes(updateTo.toUpperCase())){
          var rankTo = msg.guild.roles.cache.find(role => role.name === GetRank(updateTo).rank);
          var member = msg.mentions.members.first();
          
          if(member !== undefined){ 
            member.roles.add(rankTo).catch(console.error);
            if(rankFrom !== undefined){
              member.roles.remove(rankFrom).catch(console.error);
            }
            console.log('Rank Successfully Updated');

            var nick = member.nickname;

            var newNick = `${GetRank(updateTo, msg).abbrev}. ${ClearAllRanks(nick)}`
            member.setNickname(newNick);
            console.log(`nick update successfully: ${newNick}`)
          }
          else{
            msg.reply('No Member Defined');
          }
        }
        else{
          msg.reply(`Rank ${updateTo} Could not be found in list`);
        }
        
      }else{
        msg.reply('Sorry! You do not have permissions to edit roles')
      }
    }  
    }catch(e){
      console.log(e);
      msg.reply(`an error has occured`)
    }
  }
)

client.on("message", msg => {
  try{
    if(msg.content.startsWith("!Handle")){
      var handle = msg.content.split(/ +/);
      var newNick
      if(handle.length === 3){
        member = msg.mentions.members.first()
        newNick = handle[2];
      }
      else if(handle.length === 2){
        var member = msg.member
        newNick = handle[1];
      }

      if(member.nickname !== undefined && member.nickname !== null){
        var rank = HasRank(member.nickname)
        
        if(rank !== null){
          member.setNickname(`${rank.abbrev}. ${newNick}`);
        }
        else{
          member.setNickname(newNick);
        }
      }
      else{
        member.setNickname(newNick);
      }
    }
  }catch(e){
    console.log(e);
    //msg.reply('An Error Occured trying to set Nickname')
    msg.reply(e);
  }
})

client.on("message", msg => {
  try{
    if (msg.content.startsWith("!Promote")) {
      if(msg.member.hasPermission('MANAGE_ROLES')){
        var newRank = msg.content.split(/ +/);;
        
        var members = msg.mentions.members.array()

        for(let member of members){
          var currentRank = HasRank(member.nickname);
          var rankTo = RankByNumber(currentRank.num + parseInt(newRank[1]))
          console.log(rankTo.rank)

          var roleFrom = msg.guild.roles.cache.find(role => role.name === currentRank.rank);
          var roleTo = msg.guild.roles.cache.find(role => role.name === rankTo.rank);

          member.roles.add(roleTo).catch(console.error);
          member.roles.remove(roleFrom).catch(console.error);
          
          var nick = member.nickname;

          var newNick = `${rankTo.abbrev}. ${ClearAllRanks(nick)}`
          member.setNickname(newNick);
          console.log(`nick update successfully: ${newNick}`)
          }
      }else{
        msg.reply('Sorry! You do not have permissions to edit roles')
      }
    }  
  }catch(e){
    console.log(e);
    msg.reply(`What a muppet!
    `)
  }
})

// client.on("guildMemberAdd", (member) => {
//   let role = member.guild.roles.cache.find(role => role.name === 'Recruit');
//   member.roles.add(role).catch(console.error);
//   member.setNickName(`RCT. ${member.displayName}`).catch(console.error);
// });

function HasRank(nick){
  for(var i = 0; i< rankAbbrevs.length; i++){
    let nsplit = nick.split(/ +/);;
    if(nsplit[0] === `${rankAbbrevs[i].abbrev}.`){
      return rankAbbrevs[i]
    }
  }
  return null;
}

  function GetRank(rankName){
    for(var i = 0; i < rankAbbrevs.length; i++){
      if(rankName.toUpperCase() === rankAbbrevs[i].abbrev){
        console.log(rankAbbrevs[i]);
        return rankAbbrevs[i]
      }
    }

    return null;
  }

  function ClearAllRanks(nick){
    for(var i = 0; i < rankAbbrevs.length; i++){
      nick = nick.replace(`${rankAbbrevs[i].abbrev}. `, '');
    }

    return nick
  }

  function RankByNumber(num){
    for(var i = 0; i < rankAbbrevs.length; i++){
      if(rankAbbrevs[i].num === num){
        return rankAbbrevs[i]
      }
    }
  }
