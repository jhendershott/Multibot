require("dotenv").config()
const Discord = require("discord.js")
const client = new Discord.Client()

client.login(process.env.BotToken)
client.on("ready", () => {
  console.log(`Logged in as ${client.user.tag}!`)
})

var ranks = ['RCT', 'CDT', 'PVT', 'PV2', 'PFC', 'SPC', 'LCPL', 'CPL', 'SGT', 'SSG', 'LT2', 'LT', 'LCDR', 'CDR', 'CAPT', 'VADM', 'ADM', 'HADM']

var rankAbbrevs = [
  {rank: 'Recruit', abbrev: 'RCT' },
  {rank: 'Cadet', abbrev: 'CDT' },
  {rank: 'Private', abbrev: 'PVT' },
  {rank: 'Private Second Class', abbrev: 'PV2'},
  {rank: 'Private First Class', abbrev: 'PFC'},
  {rank: 'Specialist', abbrev: 'SPC'},
  {rank: 'Lance Corporal', abbrev: 'LCPL'},
  {rank: 'Corporal', abbrev: 'CPL'},
  {rank: 'Sergeant', abbrev: 'SGT' },
  {rank: 'Staff Sergeant', abbrev: 'SSG' },
  {rank: 'Second Lieutenant', abbrev: 'LT2' },
  {rank: 'Lieutenant', abbrev: 'LT' },
  {rank: 'Lieutenant Commander', abbrev: 'LCDR' },
  {rank: 'Commander', abbrev: 'CDR' },
  {rank: 'Captain', abbrev: 'CPT' },
  {rank: 'Vice Admiral', abbrev: 'VADM' },
  {rank: 'Admiral', abbrev: 'ADM' },
  {rank: 'High Admiral', abbrev: 'HADM' },
]

client.on("message", msg => {
    try{
      if (msg.content.includes("!UpdateRank")) {
        if(msg.member.hasPermission('MANAGE_ROLES')){
        var newRank = msg.content.split(' ');
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
    if(msg.content.includes("!Handle")){
      var handle = msg.content.split(' ')
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
    msg.reply('An Error Occured trying to set Nickname')
  }
})

client.on("message", msg => {
  if(msg.content.includes('!AddRank')){
    var rank 
    var abbrev
    var split = msg.content.split();
    var strings
    for(let i = 0; i < split.length; i++){
      if(s !== ' ' && s !== '!AddRank'){
        strings.push(split[i]);
      }
    }
    rankAbbrevs.push(strings[0], strings[1])
  }
})

// client.on("guildMemberAdd", (member) => {
//   let role = member.guild.roles.cache.find(role => role.name === 'Recruit');
//   member.roles.add(role).catch(console.error);
//   member.setNickName(`RCT. ${member.displayName}`).catch(console.error);
// });

function HasRank(nick){
  for(var i = 0; i< rankAbbrevs.length; i++){
    let nsplit = nick.split(' ');
    if(nsplit[0] === `${rankAbbrevs[i].abbrev}.`){
      return rankAbbrevs[i]
    }
  }
  return null;
}

  function GetRank(rankName){
    for(var i = 0; i < rankAbbrevs.length; i++){
      if(rankName.toUpperCase() === rankAbbrevs[i].abbrev){
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

