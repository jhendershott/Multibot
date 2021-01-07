using System;
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
            discord = new DiscordClient(new DiscordConfiguration {
                Token = Environment.GetEnvironmentVariable("BotToken2"),
                TokenType = TokenType.Bot
            });

            interactivity = discord.UseInteractivity(new InteractivityConfiguration());

            commands = discord.UseCommandsNext(new CommandsNextConfiguration() {
                StringPrefixes = new string[] { "." },
                CaseSensitive = false
            });

            commands.RegisterCommands<Commands>();

            var command = new Commands();

            //discord.MessageCreated += Discord_MessageCreated;

            await discord.ConnectAsync ();
            await Task.Delay (-1);
        }

        //private static async Task Discord_MessageCreated(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs e)
        //{
        //    var command = new Commands();
        //    await command.SkynetProtocol(e);
        //}

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            TelemetryHelper.Singleton.LogEvent("BOT STOP");
        }
    }
}
