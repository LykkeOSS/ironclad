namespace Ironclad.Console.Commands
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using McMaster.Extensions.CommandLineUtils;

    internal class AddUserClaimsCommand : ICommand
    {
        private const char ClaimValueSeparator = '=';

        private string username;
        private Dictionary<string, object> claims;

        public static void Configure(CommandLineApplication app, CommandLineOptions options)
        {
            // description
            app.Description = "Add claims to user claims list";
            app.HelpOption();

            // arguments
            var argumentUsername = app.Argument("username", "The username");
            var argumentClaims = app.Argument(
                "claims",
                "One or more claims to assign to the user (format: claim=value)",
                true);

            app.OnExecute(() =>
            {
                if (string.IsNullOrEmpty(argumentUsername.Value) || !argumentClaims.Values.Any())
                {
                    app.ShowHelp();
                    return;
                }

                var argumentClaimsSplit = argumentClaims.Values.Select(x =>
                    new KeyValuePair<string, object>(
                        x.Split(ClaimValueSeparator).First(),
                        x.Split(ClaimValueSeparator).Last()))
                    .ToList();

                if (argumentClaimsSplit.Any(x => string.IsNullOrWhiteSpace(x.Key) || x.Value == null))
                {
                    app.ShowHelp();
                    return;
                }

                options.Command = new AddUserClaimsCommand
                    { username = argumentUsername.Value, claims = new Dictionary<string, object>(argumentClaimsSplit) };
            });
        }

        public async Task ExecuteAsync(CommandContext context)
        {
            var user = await context.UsersClient.GetUserAsync(this.username).ConfigureAwait(false);

            await context.UsersClient.AddClaimsAsync(user, this.claims).ConfigureAwait(false);
        }
    }
}