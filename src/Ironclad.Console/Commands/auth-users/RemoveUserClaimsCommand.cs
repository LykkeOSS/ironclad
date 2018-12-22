// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Console.Commands
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using McMaster.Extensions.CommandLineUtils;
    using Sdk;

    internal class RemoveUserClaimsCommand : ICommand
    {
        private string username;
        private Dictionary<string, IEnumerable<object>> claims;

        public static void Configure(CommandLineApplication app, CommandLineOptions options)
        {
            // description
            app.Description = "Remove claims from the user";
            app.HelpOption();

            // arguments
            var argumentUsername = app.Argument("username", "The username");
            var argumentClaims = app.Argument(
                "claims",
                "One or more claims to remove from the user (format: claim=value)",
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

                options.Command = new RemoveUserClaimsCommand
                {
                    username = argumentUsername.Value,
                    claims = new Dictionary<string, IEnumerable<object>>(argumentClaimsSplit.ToClaims())
                };
            });
        }

        public async Task ExecuteAsync(CommandContext context)
        {
            await context.UsersClient.RemoveClaimsAsync(this.username, this.claims).ConfigureAwait(false);
        }
    }
}