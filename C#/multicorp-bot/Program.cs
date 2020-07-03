using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;

namespace multicorp_bot {
    class Program {
        static DiscordClient discord;
        static CommandsNextModule commands;

        static void Main (string[] args) {
            MainAsync (args).ConfigureAwait (false).GetAwaiter ().GetResult ();
        }

        static async Task MainAsync (string[] args) {
            discord = new DiscordClient (new DiscordConfiguration {
                Token = "NzI4MDQwNjMyMDc2OTkyNTY1.Xv-asw.1E9umf5lIfsA9xSIatzQ7KmGr1U",
                    TokenType = TokenType.Bot,
                    UseInternalLogHandler = true,
                    LogLevel = LogLevel.Debug,

            });
            commands = discord.UseCommandsNext (new CommandsNextConfiguration {
                StringPrefix = "."
            });
            commands.RegisterCommands<Commands> ();

            await discord.ConnectAsync ();
            await Task.Delay (-1);
        }
    }
}