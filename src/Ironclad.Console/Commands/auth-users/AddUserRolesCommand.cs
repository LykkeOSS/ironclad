﻿// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Console.Commands
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using McMaster.Extensions.CommandLineUtils;

    internal class AddUserRolesCommand : ICommand
    {
        private string username;
        private List<string> roles;

        private AddUserRolesCommand()
        {
        }

        public static void Configure(CommandLineApplication app, CommandLineOptions options)
        {
            // description
            app.Description = "Add roles to user roles list";
            app.HelpOption();

            // arguments
            var argumentUsername = app.Argument("username", "The username");
            var argumentRoles = app.Argument(
                "roles",
                "One or more roles to assign to the user",
                true);

            app.OnExecute(() =>
            {
                if (string.IsNullOrEmpty(argumentUsername.Value) || !argumentRoles.Values.Any())
                {
                    app.ShowHelp();
                    return;
                }

                options.Command = new AddUserRolesCommand
                    { username = argumentUsername.Value, roles = argumentRoles.Values };
            });
        }

        public async Task ExecuteAsync(CommandContext context)
        {
            var user = await context.UsersClient.GetUserAsync(this.username).ConfigureAwait(false);

            await context.UsersClient.AddRolesAsync(user, this.roles).ConfigureAwait(false);
        }
    }
}