// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Console.Commands
{
    using McMaster.Extensions.CommandLineUtils;

    internal static class ModifyUserRolesOptions
    {
        public static void Configure(CommandLineApplication app, CommandLineOptions options)
        {
            // description
            app.Description = "Modify user roles";
            app.HelpOption();

            // commands
            app.Command("add", command => AddUserRolesCommand.Configure(command, options));
            app.Command("remove", command => RemoveUserRolesCommand.Configure(command, options));

            app.OnExecute(app.ShowVersionAndHelp);
        }
    }
}