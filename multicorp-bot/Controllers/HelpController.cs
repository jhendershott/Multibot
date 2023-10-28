using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace multicorp_bot.Controllers
{
    public static class HelpController
    {
        public static async Task SelectHelpEmbed(DiscordChannel channel, string HelpType)
        {
            switch (HelpType.ToLower())
            {
                case "bank":
                    await channel.SendMessageAsync(embed: BankEmbed());
                    break;
                case "loans":
                    await channel.SendMessageAsync(embed: LoanEmbed());
                    break;
                case "handle":
                    await channel.SendMessageAsync(embed: HandleEmbed());
                    break;
                case "fleet":
                    await channel.SendMessageAsync(embed: FleetEmbed());
                    break;
                case "wipe":
                    await channel.SendMessageAsync(embed: WipeHelper());
                    break;
                case "dispatch":
                    await channel.SendMessageAsync(embed: DispatchEmbed());
                    break;
                case "log":
                    await channel.SendMessageAsync(embed: LogEmbed());
                    break;
            }
        }

        public static DiscordEmbed BankEmbed()
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();

            builder.Title = "Bank Helper";
            builder.Description = "remember all commands are case insensitive";
            builder.AddField("Setup", "*Please set up a channel in your server named Bank your balance will be updated in that channel");
            builder.AddField("Deposit", "You can Deposit for yourself or on someone else behalf \n" +
                "Try !Bank deposit 5000 \n" +
                "or !Bank deposit @{another member} 5000");
            builder.AddField("WithDraw", "This removes money from the bank *Note* only bankers can withdraw\n" +
                "!Bank Withdraw 5000");
           
            return builder.Build();
        }

        public static DiscordEmbed LoanEmbed()
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();

            builder.Title = "Loan Helper";
            builder.AddField("Request", "!Loan Request If you request a loan you can follow the prompts to create a new loan request");
            builder.AddField("View", "!Loan View will allow you to see all the current pending loans");
            builder.AddField("Payment", "!Loan Payment will give you prompts to make a payment " +
                "\n     *Note* make sure your funding partner is online to confirm your payment");
            builder.AddField("Fund", "!Loan Fund allows you to accept underwriting of the loan. " +
                "\n     Once you have accepted it, please reach out to the individual to transfer the funds");
            builder.AddField("Complete", "!Loan Complete is used when you have finally paid off the loan");

            return builder.Build();
        }

        public static DiscordEmbed HandleEmbed()
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            builder.Title = "Handle Helper";
            builder.Description = "This may cause issues if you don't have roles or ranks";

            builder.AddField("For yourself", "This is for you to change your discord nickname to match your Star Citizen Hanlde \n" +
                "!Handle {new nickname}");
            builder.AddField("For someone else", "This is for you to change another members discord nickname to match your Star Citizen Hanlde \n" +
                "!Handle @{tag member} {new nickname}");

            return builder.Build();
        }

        public static DiscordEmbed WipeHelper()
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            builder.Title = "Wipe -- Don't you fuckin' do it :exploding_head: :gun:";

            return builder.Build();
        }

        public static DiscordEmbed FleetEmbed()
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();

            builder.Title = "Fleet Helper";
            builder.Description = "Used to create and manage Fleet requests. Officers can request ships necessary to our fleet and the org may join in";
            builder.AddField("view", "This will display the current and pending ship requests\n" +
                "!Fleet view");
            builder.AddField("Fund", "This will apply the funds directly to the ship request. You will need to transfer the money to the bank\n" 
                + "You will be walked through the steps to complete the transaction \n"
                + "!Fleet Fund");
            builder.AddField("Complete", "This will close all ship requests in your org that have reached a zero balance\n"
                + "!Fleet Complete");


            return builder.Build();
        }

        public static DiscordEmbed DispatchEmbed()
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();

            builder.Title = "Dispatch Helper";
            builder.Description = "This is your one stop shop for all things dispatch related, adding, viewing and accepting dispatckes";
            builder.AddField("Setup", "Please create a channel called job-board, this will be updated with changing jobs");
            builder.AddField("Dispatch", "Dispatch will give you a random work order from the type you choose. currently supported types are: \n" +
                "Trading, shipping, mining, roc mining, and hand mining\n" + 
            "!dispatch or !dispatch mining"); ;
            builder.AddField("Add", "This will walk you through the steps to add a new dispatch. \n" +
                " Dispatches are used for earning XP or accomplishing org goals\n" +
                "!dispatch Add");
            builder.AddField("View", "Dispatch view will show a list of your accepted work orders and the remaining materials \n"
                + "!Dispatch view");
            builder.AddField("Accept", "To accept a dispatch simply click the accept button underneath the job type in your job-board channel");
            builder.AddField("Log", "You will be asked 3 questions, \n 1. the work order id \n" +
                "2. the material you're delivering, such as hadanite, not the id\n " +
                "3. The amount of units you delivered\n"+
                "!Dispatch Log");


            return builder.Build();
        }

        public static DiscordEmbed LogEmbed()
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();

            builder.Title = "Log";
            builder.Description = "This is your work order log. Use this to complete your work order";
            builder.AddField("Log", "You will be asked 3 questions, \n 1. the work order id \n" +
                "2. the material you're delivering, such as hadanite, not the id\n " +
                "3. The amount of units you delivered\n" +
                "!Log");
            builder.AddField("!Dispatch Log", "Dispatch log works the same way, as Log, it just gives you another entry point"
                + "!Dispatch Log");
            builder.AddField("Complete", "This will close all ship requests in your org that have reached a zero balance\n"
                + "!Fleet Complete");


            return builder.Build();
        }
    }
}
