using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using multicorp_bot.Helpers;

namespace multicorp_bot {
    class Program {
        static DiscordClient discord;
        
       static async Task Main (string[] args) {
            //string token = Environment.GetEnvironmentVariable("BOTTOKEN");
            string token = "NjkzODcwNzM3OTExNDQ3NjEy.GIbCnR.V-zOZBhTJSx7nZ9lmPhJR1G2gbLx4gLbjqEVco";
            discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All,
                MinimumLogLevel = LogLevel.Debug
            });

            discord.ComponentInteractionCreated += async (s, e) =>
            {
                new ComponentInteractions(s, e).Parse();
                await e.Interaction.CreateResponseAsync(InteractionResponseType.Pong);
            };

            discord.UseInteractivity(new InteractivityConfiguration()
            {
                PollBehaviour = PollBehaviour.KeepEmojis,
                Timeout = TimeSpan.FromSeconds(60)
            });

            var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
                {
                    StringPrefixes = new[] { "!" },
                    CaseSensitive = false,
                    EnableDefaultHelp = false,
                }
            );

            commands.RegisterCommands<Commands>();

            await discord.ConnectAsync ();
            await Task.Delay (-1);
        }

        //private static async Task Discord_MessageCreated(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs e)
        //{
        //    var command = new Commands();
        //    string[] messageStrings = new string[] { "bot", "multibot" };
        //    var strArray = e.Message.Content.Split(" ");
        //    string[] prohibChannelArr = new string[]
        //    {
        //        "command-chat",
        //        "officer-quarters",
        //        "op-planning",
        //        "frontline-news",
        //        "war-room-rp",
        //        "dispatch-rp",
        //        "war-assets-rp",
        //        "meta"
        //    };

        //    if ((e.Guild.Name == "MultiCorp" || e.Guild.Name == "Man vs Owlbear") && e.Author.Username != "MultiBot" && !prohibChannelArr.Contains(e.Channel.Name)) 
        //    {
        //        if (strArray.Intersect(messageStrings).Any() || e.MentionedUsers.Any(x => x.Username == "MultiBot"))
        //        {
        //            await Task.Run(() => command.SkynetProtocol(e));
        //        }
        //    }
        //    else
        //    {
        //        await Task.CompletedTask;
        //    }       
        //}

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Console.WriteLine("Bot Stop");
        }
    }
}
