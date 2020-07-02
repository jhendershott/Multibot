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
                Token = "NzI4MDQwNjMyMDc2OTkyNTY1.Xv0m5Q.C3nRIzgjrjfwCPPUoEugnGo-i8k",
                    TokenType = TokenType.Bot,
                    UseInternalLogHandler = true,
                    LogLevel = LogLevel.Debug,

            });
            commands = discord.UseCommandsNext (new CommandsNextConfiguration {
                StringPrefix = ";;"
            });
            commands.RegisterCommands<Commands> ();

            discord.MessageCreated += async e => {
                if (e.Message.Content.ToLower ().StartsWith ("ping"))
                    await e.Message.RespondAsync ("pong!");
            };

            await discord.ConnectAsync ();
            await Task.Delay (-1);
        }
    }
}