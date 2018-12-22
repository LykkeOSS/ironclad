// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Console.Commands
{
    using System.Linq;
    using System.Threading.Tasks;
    using Client;
    using McMaster.Extensions.CommandLineUtils;

    internal class UpdateUserRolesCommand : ICommand
    {
        private string username;
        private CollectionUpdateMethod<string> rolesUpdateMethod;

        private UpdateUserRolesCommand()
        {
        }

        public static void Configure(CommandLineApplication app, CommandLineOptions options)
        {
            // description
            app.Description = "Update user roles";
            app.HelpOption();

            // arguments
            var argumentUsername = app.Argument("username", "The username", false);
            var argumentRoles = app.Argument("roles",
                "One or more roles to assign to the user (you can specify multiple roles)", true);

            var optionRemove = app.Option("-r|--remove", "Removes all the roles", CommandOptionType.NoValue);
            var optionAdd = app.Option("-a|--add", "Add roles", CommandOptionType.NoValue);

            // action (for this command)
            app.OnExecute(
                () =>
                {
                    if (string.IsNullOrEmpty(argumentUsername.Value) ||
                        (!optionRemove.HasValue() && !argumentRoles.Values.Any()) ||
                        (!optionAdd.HasValue() && !argumentRoles.Values.Any()))
                    {
                        app.ShowHelp();
                        return;
                    }

                    options.Command = new UpdateUserRolesCommand
                    {
                        username = argumentUsername.Value,
                        rolesUpdateMethod = CollectionUpdateMethod<string>.FromOptions(
                            optionAdd.HasValue(),
                            optionRemove.HasValue(),
                            argumentRoles.Values)
                    };
                });
        }

        public async Task ExecuteAsync(CommandContext context)
        {
            var user = await context.UsersClient.GetUserAsync(this.username).ConfigureAwait(false);

            var update = new User
            {
                Username = this.username,
                Roles = this.rolesUpdateMethod.ApplyTo(user.Roles),
                Claims = null,
            };

            await context.UsersClient.ModifyUserAsync(update).ConfigureAwait(false);
        }
    }
}