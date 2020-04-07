var Bank = require("./Bank");
var Ranks = require("./Ranks");

const Discord = require("discord.js")
const client = new Discord.Client()

const rankUtilities = new Ranks();
const bank = new Bank();

client.login(process.env.BotToken)

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
          rankFrom = msg.guild.roles.cache.find(role => role.name === rankUtilities.GetRank(updateFrom).rank);
        }
        else {
          updateTo = newRank[2];
        }

        if(rankUtilities.rankList.includes(updateTo.toUpperCase())){
          var rankTo = msg.guild.roles.cache.find(role => role.name === rankUtilities.GetRank(updateTo).rank);
          var member = msg.mentions.members.first();
          
          if(member !== undefined){ 
            member.roles.add(rankTo).catch(console.error);
            if(rankFrom !== undefined){
              member.roles.remove(rankFrom).catch(console.error);
            }
            console.log('Rank Successfully Updated');

            var nick = member.nickname;

            var newNick = `${rankUtilities.GetRank(updateTo, msg).abbrev}. ${rankUtilities.ClearAllRanks(nick)}`
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
        msg.reply("You can't change roles, who do you think ya are? Ya MUPPET!")
      }
    }  
    }catch(e){
      console.log(e);
      msg.reply(`WNR's the muppet and screwed something up`)
    }
  }
)

client.on("message", msg => {
  try{
    if(msg.content.startsWith("!Handle")){
      var handleCmd = msg.content.split(/ +/);
      var newNick
      if(handleCmd.length === 3){
        member = msg.mentions.members.first()
        newNick = handleCmd[2];
      }

      else if(handleCmd.length === 2){
        var member = msg.member
        newNick = handleCmd[1];
      }

      if(member.nickname !== undefined && member.nickname !== null){
        var rank = rankUtilities.HasRank(member.nickname)
        
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
    msg.reply(`WNR's the muppet and screwed something up`)
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
          var currentRank = rankUtilities.HasRank(member.nickname);
          var rankTo = rankUtilities.RankByNumber(currentRank.num + parseInt(newRank[1]))
          console.log(rankTo.rank)

          var roleFrom = msg.guild.roles.cache.find(role => role.name === currentRank.rank);
          var roleTo = msg.guild.roles.cache.find(role => role.name === rankTo.rank);

          member.roles.add(roleTo).catch(console.error);
          member.roles.remove(roleFrom).catch(console.error);
          
          var nick = member.nickname;

          var newNick = `${rankTo.abbrev}. ${rankUtilities.ClearAllRanks(nick)}`
          member.setNickname(newNick);
          console.log(`nick update successfully: ${newNick}`)
          }
      }else{
        msg.reply("You can't change roles, who do you think ya are? Ya MUPPET!")
      }
    }  
  }catch(e){
    console.log(e);
    msg.reply(`WNR's the muppet and screwed something up`)
  }
})

client.on("message",async msg => {
  try{
    if (msg.content.startsWith("!Bank")) {
      var args = msg.content.split(/ +/);;
      if(args[1].toLowerCase() === 'balance'){
        var balance = await bank.GetBalanceNewConnect();
        msg.channel.send(`${balance} aUEC`)
      }

      else if(args[1].toLowerCase() === 'deposit' && args.length === 3){
        var deposit = await bank.Deposit(rankUtilities.ClearAllRanks(msg.member.nickname), args[2]);
        msg.channel.send(`Thank you for your contribution: New Balance = ${deposit} aUEC`)
      }
      else if(args[1].toLowerCase() === 'deposit' && args.length === 4){
        var deposit = await bank.Deposit(rankUtilities.ClearAllRanks(msg.mentions.members.first().nickname), args[2]);
        msg.channel.send(`Thank you for your contribution: New Balance = ${deposit} aUEC`)
      }

      if(args[1].toLowerCase() === 'contribution' && args.length === 2){
        var nickname = msg.member.nickname
        var cleanedNick = rankUtilities.ClearAllRanks(nickname);
        var memberId = await bank.GetMemberId(cleanedNick);
        if(!memberId){
          await bank.AddMember(cleanedNick);
          msg.channel.send(`It appears you are not on our books but have been to the ledger`);
        }else{
          console.log(memberId);
          var transid = await bank.GetTransactionId(memberId)
          if(transid){
            msg.channel.send(`Total Contributions from ${nickname} = ${await bank.GetTransactionAmount(transid)} aUEC`)
          }
          else{
            msg.channel.send(`You have not contributed to MultiCorp Bank`)
          }  
        }      
      } 
      
      else if(args[1].toLowerCase() === 'contribution' && args.length === 3){
        var nickname = msg.mentions.members.first().nickname
        var cleanedNick = rankUtilities.ClearAllRanks(nickname);
        var memberId = await bank.GetMemberId(cleanedNick);
        if(!memberId){
          await bank.AddMember(cleanedNick);
          msg.channel.send(`It appears ${nickname} isn't on our books but has been to the ledger`);
        }else{
          console.log(memberId);
          var transid = await bank.GetTransactionId(memberId)
          if(transid){
            msg.channel.send(`Total Contributions from ${nickname} = ${await bank.GetTransactionAmount(transid)} aUEC`)
          }
          else{
            msg.channel.send(`${nickname} has not contributed to MultiCorp Bank`)
          }  
        }
      }

      if(args[1].toLowerCase() === 'withdraw'){
        if(msg.member.roles.cache.find(r => r.name === "Banker")){
          var withdraw = await bank.Withdraw(args[2]);
          msg.channel.send(`You have withdrawn funds: New Balance = ${withdraw} aUEC`)
        }
        else{
          msg.channel.send("Come on, muppets like you can't pull out money willy nilly!");
        }
      }
    }

  } catch (e){
    console.log(e)
  }
})

client.on("message",async msg => {
  try{
    var args = msg.content.split(/ +/);;
    if (msg.content.startsWith("!Help") && args.length === 1) {
      msg.channel.send("MultiBot is your one stop shop for all your needs");
      msg.channel.send("Try out !Handle {new handle name} will update your your name while keeping your rank");
      msg.channel.send("Try !Help Promote !Promote will manage members roles ");
      msg.channel.send("Try !Help Bank will help you manage the org bank");
    }
    else if (msg.content.startsWith("!Help") && args[1] === 'Bank') {
      msg.channel.send("!Bank Depost {amount} - will add to the account and your contributions");
      msg.channel.send("try out !Bank Depost {amount} {tagged server member} - will add to the account and their contributions");
      msg.channel.send("try out !Bank Contribution - will display your total contributions to the bank");
      msg.channel.send("try out !Bank Contribution {tagged server name} - will display their total contributions to the bank");
      msg.channel.send("try out !Bank Withdraw - will withdraw funds from the org bank *Note only Bankers are allowed to withdraw*");  
    }
    
    else if (msg.content.startsWith("!Help") && args[1] === 'Promote') {
      msg.channel.send("try out !Promote {how many ranks} {tagged server members} - will increase ranks of member");
      msg.channel.send("This will update their rank on the server as well as their nickname");
      msg.channel.send("Only members with MANAGE_ROLES permissions can promote");
    }
  } catch (e){
    console.log(e)
  }
})


// client.on("guildMemberAdd", (member) => {
//   let role = member.guild.roles.cache.find(role => role.name === 'Recruit');
//   member.roles.add(role).catch(console.error);
//   member.setNickName(`RCT. ${member.displayName}`).catch(console.error);
// });

