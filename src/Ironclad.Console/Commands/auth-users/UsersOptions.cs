// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Console.Commands
{
    using Client;
    using McMaster.Extensions.CommandLineUtils;

    // NOTE (Cameron): This command is informational only and cannot be executed (only 'show help' works) so inheriting ICommand is unnecessary.
    internal static class UsersOptions
    {
        public static void Configure(CommandLineApplication app, CommandLineOptions options, IConsole console)
        {
            // description
            app.Description = "Manage users";
            app.HelpOption();

            // commands
            app.Command("add", command => AddUserCommand.Configure(command, options, console));
            app.Command("remove", command => RemoveCommand.Configure(command, options, GetRemoveCommandOptions()));
            app.Command("show", command => ShowCommand.Configure(command, options, GetShowCommandOptions()));
            app.Command("modify", command => ModifyUserCommand.Configure(command, options));
            app.Command("roles", command => RolesOptions(command, options));
            app.Command("claims", command => ClaimsOptions(command, options));

            // action (for this command)
            app.OnExecute(app.ShowVersionAndHelp);
        }

        private static RemoveCommandOptions GetRemoveCommandOptions() =>
            new RemoveCommandOptions
            {
                Type = "user",
                ArgumentName = "username",
                ArgumentDescription = "The username of the user to remove",
                RemoveCommand = value => new RemoveCommand(async context => await context.UsersClient.RemoveUserAsync(value).ConfigureAwait(false)),
            };

        private static ShowCommandOptions GetShowCommandOptions() =>
            new ShowCommandOptions
            {
                Type = "user",
                ArgumentName = "username",
                ArgumentDescription = "The username (you can end the username with a wildcard to search)",
                DisplayCommand = (string value) => new ShowCommand.Display<User>(async context => await context.UsersClient.GetUserAsync(value).ConfigureAwait(false)),
                ListCommand = (string startsWith, int skip, int take) =>
                    new ShowCommand.List<UserSummary>(
                        "users",
                        async context => await context.UsersClient.GetUserSummariesAsync(startsWith, skip, take).ConfigureAwait(false),
                        ("username", user => user.Username),
                        ("email", user => user.Email),
                        ("sub", user => user.Id)),
            };

        private static void RolesOptions(CommandLineApplication app, CommandLineOptions options)
        {
            // description
            app.Description = "Modify user roles";
            app.HelpOption();

            // commands
            app.Command("add", command => AddUserRolesCommand.Configure(command, options));
            app.Command("remove", command => RemoveUserRolesCommand.Configure(command, options));

            app.OnExecute(app.ShowVersionAndHelp);
        }

        private static void ClaimsOptions(CommandLineApplication app, CommandLineOptions options)
        {
            // description
            app.Description = "Modify user claims";
            app.HelpOption();

            // commands
            app.Command("add", command => AddUserClaimsCommand.Configure(command, options));
            app.Command("remove", command => RemoveUserClaimsCommand.Configure(command, options));

            app.OnExecute(app.ShowVersionAndHelp);
        }
    }
}