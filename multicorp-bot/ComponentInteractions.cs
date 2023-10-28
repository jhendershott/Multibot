using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using multicorp_bot.Controllers;

namespace multicorp_bot
{
	public class ComponentInteractions
	{
		public ComponentInteractions(DiscordClient client, ComponentInteractionCreateEventArgs eventArgs)
		{
			this.DiscordClient = client;
			this.EventArgs = eventArgs;
			var idArr = eventArgs.Id.Split("-");
			this.ComponentAction = idArr[0];
			this.ComponentId = idArr[1];
        }

		public DiscordClient DiscordClient;
		public ComponentInteractionCreateEventArgs EventArgs;
		public String ComponentAction;
		public String ComponentId;

		public async void Parse()
		{
			switch (this.ComponentAction)
			{
				case "accept_order":
					new WorkOrderController().AcceptWorkOrder(EventArgs.User, EventArgs.Guild, EventArgs.Channel, ComponentId);
					await new Commands().UpdateJobBoard(EventArgs.Guild, EventArgs.Channel);
					break;
					
				case "help":
					await HelpController.SelectHelpEmbed(EventArgs.Channel, ComponentId);
					break;
            }
		}
	}
}

