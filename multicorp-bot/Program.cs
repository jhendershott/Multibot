using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using multicorp_bot.Helpers;

namespace multicorp_bot {
    class Program {
        static DiscordClient discord;
        static CommandsNextExtension commands;
        static InteractivityExtension interactivity;
        

        static void Main (string[] args) {
            TelemetryHelper.Singleton.LogEvent("BOT START");
            MainAsync (args).ConfigureAwait (false).GetAwaiter ().GetResult ();
        }

        static async Task MainAsync (string[] args) {
            string token = Environment.GetEnvironmentVariable("BOTTOKEN");
            discord = new DiscordClient(new DiscordConfiguration {
                Token = token,
                TokenType = TokenType.Bot
            });

            interactivity = discord.UseInteractivity(new InteractivityConfiguration());

            commands = discord.UseCommandsNext(new CommandsNextConfiguration() {
                StringPrefixes = new string[] { "!" },
                CaseSensitive = false
            });

            commands.RegisterCommands<Commands>();

            var command = new Commands();

            await Task.Run(() => discord.MessageCreated += Discord_MessageCreated);

            await discord.ConnectAsync ();
            await Task.Delay (-1);
        }

        private static async Task Discord_MessageCreated(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            var command = new Commands();
            string[] messageStrings = new string[] { "bot", "multibot" };
            var strArray = e.Message.Content.Split(" ");
            string[] prohibChannelArr = new string[]
            {
                "command-chat",
                "officer-quarters",
                "op-planning",
                "frontline-news",
                "war-room-rp",
                "dispatch-rp",
                "war-assets-rp",
                "meta"
            };

            if ((e.Guild.Name == "MultiCorp" || e.Guild.Name == "Man vs Owlbear") && e.Author.Username != "MultiBot" && !prohibChannelArr.Contains(e.Channel.Name)) 
            {
                if (strArray.Intersect(messageStrings).Any() || e.MentionedUsers.Any(x => x.Username == "MultiBot"))
                {
                    await Task.Run(() => command.SkynetProtocol(e));
                }
            }
            else
            {
                await Task.CompletedTask;
            }       
        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            TelemetryHelper.Singleton.LogEvent("BOT STOP");
        }
    }
}
