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
			DiscordClient = client;
			EventArgs = eventArgs;
			var idArr = eventArgs.Id.Split("-");
			ComponentAction = idArr[0];
			ComponentId = idArr[1];
        }

		public DiscordClient DiscordClient;
		public ComponentInteractionCreateEventArgs EventArgs;
		public string ComponentAction;
		public string ComponentId;

		public async void Parse()
		{
			switch (ComponentAction)
			{
				case "accept_order":
					new WorkOrderController().AcceptWorkOrder(EventArgs.User, EventArgs.Guild, EventArgs.Channel, ComponentId);
					await new Commands().UpdateJobBoard(EventArgs.Guild, EventArgs.Channel);
					break;
				case "help":
					await HelpController.SelectHelpEmbed(EventArgs.Channel, ComponentId);
					break;
				case "expense":
					await new BankController().ExpenseButtonInteractionAsync(ComponentId, EventArgs.Guild, EventArgs.Channel);
					break;
            }
		}
	}
}

