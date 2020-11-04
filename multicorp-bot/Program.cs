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
                Token = Environment.GetEnvironmentVariable("BOTTOKEN"),
                TokenType = TokenType.Bot
            });

            interactivity = discord.UseInteractivity(new InteractivityConfiguration());

            commands = discord.UseCommandsNext(new CommandsNextConfiguration() {
                StringPrefixes = new string[] { "!" },
                CaseSensitive = false
            });

            commands.RegisterCommands<Commands>();

            await discord.ConnectAsync ();
            await Task.Delay (-1);
        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            TelemetryHelper.Singleton.LogEvent("BOT STOP");
        }
    }
}
