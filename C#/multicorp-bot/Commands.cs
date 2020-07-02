namespace multicorp_bot {
    public class Commands {
        [Command ("hi")]
        public async Task Hi (CommandContext ctx) {
            await ctx.RespondAsync ($"👋 Hi, {ctx.User.Mention}!");
        }
    }
}