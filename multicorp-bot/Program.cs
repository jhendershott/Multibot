using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using multicorp_bot.Helpers;

namespace multicorp_bot {
    class Program {
        static DiscordClient discord;
        static CommandsNextModule commands;
        static InteractivityModule interactivity;

        static void Main (string[] args) {
            TelemetryHelper.Singleton.LogEvent("BOT START");
            MainAsync (args).ConfigureAwait (false).GetAwaiter ().GetResult ();
        }

        static async Task MainAsync (string[] args) {
            discord = new DiscordClient (new DiscordConfiguration {
                Token = Environment.GetEnvironmentVariable("BotToken2"),
                    TokenType = TokenType.Bot,
                    UseInternalLogHandler = true,
                    LogLevel = LogLevel.Debug
            });
            interactivity = discord.UseInteractivity(
                new InteractivityConfiguration()
                {
                    Timeout = TimeSpan.FromMinutes(1),
                    PaginationTimeout = TimeSpan.FromMinutes(1),
                    PaginationBehaviour = TimeoutBehaviour.Ignore
                });

            commands = discord.UseCommandsNext(new CommandsNextConfiguration {
                StringPrefix = ".",
                CaseSensitive = false
            });
            commands.RegisterCommands<Commands> ();

            await discord.ConnectAsync ();
            await Task.Delay (-1);
        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            TelemetryHelper.Singleton.LogEvent("BOT STOP");
        }
    }
}
