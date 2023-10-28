using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace multicorp_bot.Controllers
{
	static class ErrorController
	{
		public static async Task<DiscordChannel> GetErrorChannel(DiscordGuild guild)
		{
			return (await guild.GetChannelsAsync()).FirstOrDefault(x => x.Name == "bot-error");
		}

		public static async void SendError(DiscordChannel channel, string errorText, DiscordGuild guild)
		{
			DiscordChannel errChan = await GetErrorChannel(guild);
			if (errChan != null)
			{
				await channel.SendMessageAsync("Something went wrong, check error channel");
				await errChan.SendMessageAsync($"I encountered an error - {errorText}");
			}
			else
			{
                await channel.SendMessageAsync($"For a cleaner experience please create a bot-error channel");
                await channel.SendMessageAsync($"I encountered an error please let WNR know - {errorText}");
			}
		}
	}
}

