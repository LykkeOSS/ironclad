namespace Ironclad.Console.Commands
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using McMaster.Extensions.CommandLineUtils;

    internal class RemoveUserClaimsCommand : ICommand
    {
        private string username;
        private List<string> claims;

        public static void Configure(CommandLineApplication app, CommandLineOptions options)
        {
            // description
            app.Description = "Remove claims from user roles list";
            app.HelpOption();

            // arguments
            var argumentUsername = app.Argument("username", "The username");
            var argumentClaims = app.Argument(
                "claims",
                "One or more claims to remove from the user's account",
                true);

            app.OnExecute(() =>
            {
                if (string.IsNullOrEmpty(argumentUsername.Value) || !argumentClaims.Values.Any())
                {
                    app.ShowHelp();
                    return;
                }

                options.Command = new RemoveUserClaimsCommand
                    { username = argumentUsername.Value, claims = argumentClaims.Values };
            });
        }

        public async Task ExecuteAsync(CommandContext context)
        {
            var user = await context.UsersClient.GetUserAsync(this.username).ConfigureAwait(false);

            await context.UsersClient.RemoveClaimsAsync(user, this.claims).ConfigureAwait(false);
        }
    }
}