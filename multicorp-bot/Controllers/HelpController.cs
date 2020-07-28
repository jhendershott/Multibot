﻿using DSharpPlus.Entities;

namespace multicorp_bot.Controllers
{
    public static class HelpController
    {
     
        public static DiscordEmbed BankEmbed()
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();

            builder.Title = "Bank Helper";
            builder.Description = "remember all commands are case insensite";
            builder.AddField("Deposit", "You can Deposit for yourself or on someone else behalf \n" +
                "Try !Bank deposit 5000 \n" +
                "or !Bank deposit @{another member} 5000");
            builder.AddField("WithDraw", "This removes money from the bank *Note* only bankers can withdraw\n" +
                "!Bank Withdraw 5000");
            builder.AddField("Balance", "Shows you the balance in your bank\n" +
                "!Bank Balance");

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

        public static DiscordEmbed PromotionEmbed()
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();

            builder.Title = "Promotion Helper";
            builder.Description = "*NOTE* this only functions for MultiCorp Military Branch";
            builder.AddField("Promote", "When you want to promote members for exceptional work on the battlefield\n" +
                "!Promote @{member1} @{member2} etc");
            builder.AddField("Promote", "When you want to demote members  for exceptional muppetry on the battlefield\n" +
                "!Demote @{member1} @{member2} etc");

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
    }
}
