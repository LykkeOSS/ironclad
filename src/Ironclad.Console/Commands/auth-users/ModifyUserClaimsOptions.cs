// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Console.Commands
{
    using McMaster.Extensions.CommandLineUtils;

    internal static class ModifyUserClaimsOptions
    {
        public static void Configure(CommandLineApplication app, CommandLineOptions options)
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