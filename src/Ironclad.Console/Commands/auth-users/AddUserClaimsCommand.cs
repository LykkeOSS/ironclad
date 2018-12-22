// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Console.Commands
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using McMaster.Extensions.CommandLineUtils;
    using Sdk;

    internal class AddUserClaimsCommand : ICommand
    {
        private string username;
        private Dictionary<string, IEnumerable<object>> claims;

        public static void Configure(CommandLineApplication app, CommandLineOptions options)
        {
            // description
            app.Description = "Add claims to the user";
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

                var argumentClaimsSplit = argumentClaims.Values
                    .Select(x => x.ToKeyValuePair())
                    .ToList();

                if (argumentClaimsSplit.Any(x => string.IsNullOrWhiteSpace(x.Key) || x.Value == null))
                {
                    app.ShowHelp();
                    return;
                }

                options.Command = new AddUserClaimsCommand
                {
                    username = argumentUsername.Value,
                    claims = new Dictionary<string, IEnumerable<object>>(argumentClaimsSplit.ToClaims())
                };
            });
        }

        public async Task ExecuteAsync(CommandContext context)
        {
            await context.UsersClient.AddClaimsAsync(this.username, this.claims).ConfigureAwait(false);
        }
    }
}