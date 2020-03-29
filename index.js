require("dotenv").config()
const Discord = require("discord.js")
const client = new Discord.Client()

client.login(process.env.BotToken)
client.on("ready", () => {
  console.log(`Logged in as ${client.user.tag}!`)
})

var ranks = ['private', 'sergeant', 'staff sergeant', 'private first class']

var rankAbbrevs = [
  {rank: 'staff sergeant', abbrev: 'SSGT.' },
  {rank: 'private first class', abbrev: 'PVT1.'},
  {rank: 'private', abbrev: 'PVT.' },
  {rank: 'sergeant', abbrev: 'SGT.' }
]

client.on("message", msg => {
  if(msg.member.hasPermission('MANAGE_ROLES')){
    try{
      if (msg.content.includes("!UpdateRank")) {
        var newRank = msg.content.split(',');
        var updateTo = newRank[2]
        var updateFrom = newRank[3]

        if(ranks.includes(updateTo.toLowerCase())){
          var rankTo = msg.guild.roles.cache.find(role => role.name === updateTo);
          var rankFrom = msg.guild.roles.cache.find(role => role.name === updateFrom);
          var member = msg.mentions.members.first();
          
          if(member !== undefined){ 
            member.roles.add(rankTo).catch(console.error);
            if(rankFrom !== undefined){
              member.roles.remove(rankFrom).catch(console.error);
            }
            msg.reply('Rank Successfully Updated');

            var nick = member.nickname;

            var newNick = `${GetRank(updateTo, msg).abbrev} ${ClearAllRanks(nick)}`
            member.setNickname(newNick);
            msg.reply(`nick update successfully: ${newNick}`)
          }
          else{
            msg.reply('No Member Defined');
          }
        }
        else{
          msg.reply(`Rank ${updateTo} Could not be found in list`);
        }
      }
    }catch(e){
      msg.reply(`an error has occured`)
    }
  }
  else{
    msg.reply('Sorry! You do not have permissions to edit roles')
  }
})

client.on("message", msg => {
  try{
    if(msg.content.includes("!Handle")){
      var member = msg.mentions.members.first();
      var handle = msg.content.split(',')[2];

      if(member.nickname !== undefined){
        var rank = HasRank(member.nickname)
        if(rank !== null){
          var nick = member.nickname.split();
          member.setNickname(`${rank.abbrev} ${nick[1]}`);
        }
      }
      else{
        member.setNickname(handle);
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

function HasRank(nick){
  for(var i = 0; i< rankAbbrevs.length; i++){
    if(nick.toLowerCase() === rankAbbrevs[i].abbrev){
      return rankAbbrevs[i]
    }
    else{
      return null;
    }
  }
}

  function GetRank(rankName){
    for(var i = 0; i < rankAbbrevs.length; i++){
      if(rankName.toLowerCase() === rankAbbrevs[i].rank){
        return rankAbbrevs[i]
      }
    }

    return null;
  }

  function ClearAllRanks(nick){
    for(var i = 0; i < rankAbbrevs.length; i++){
      nick = nick.replace(rankAbbrevs[i].abbrev, '');
    }

    return nick
  }

