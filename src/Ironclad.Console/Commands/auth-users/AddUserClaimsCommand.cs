// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Console.Commands
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using McMaster.Extensions.CommandLineUtils;

    internal class AddUserClaimsCommand : ICommand
    {
        private string username;
        private List<KeyValuePair<string, object>> claims;

        public static void Configure(CommandLineApplication app, CommandLineOptions options)
        {
            // description
            app.Description = "Add claims to the user";
            app.HelpOption();

            // arguments
            var argumentUsername = app.Argument("username", "The username");
            var argumentClaims = app.Argument("claims", "One or more claims to assign to the user (format: claim=value)", true);

            app.OnExecute(() =>
            {
                if (string.IsNullOrEmpty(argumentUsername.Value) || !argumentClaims.Values.Any())
                {
                    app.ShowHelp();
                    return;
                }

                var claims = argumentClaims.Values.Select(value => new KeyValuePair<string, object>(value.Split('=').First(), value.Split('=').Last())).ToList();
                if (claims.Any(kvp => string.IsNullOrWhiteSpace(kvp.Key) || kvp.Value == null))
                {
                    app.ShowHelp();
                    return;
                }

                options.Command = new AddUserClaimsCommand
                {
                    username = argumentUsername.Value,
                    claims = claims,
                };
            });
        }

        public async Task ExecuteAsync(CommandContext context) => await context.UsersClient.AddClaimsAsync(this.username, this.claims).ConfigureAwait(false);
    }
}