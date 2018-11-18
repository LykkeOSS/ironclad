// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Console.Commands
{
    using System.Globalization;
    using Ironclad.Client;
    using McMaster.Extensions.CommandLineUtils;

    // NOTE (Cameron): This command is informational only and cannot be executed (only 'show help' works) so inheriting ICommand is unnecessary.
    internal static class IdentityProvidersOptions
    {
        public static void Configure(CommandLineApplication app, CommandLineOptions options, IConsole console)
        {
            // description
            app.Description = $"Manage identity providers";
            app.HelpOption();

            // commands
            app.Command("add", command => AddIdentityProviderCommand.Configure(command, options, console));
            app.Command("remove", command => RemoveCommand.Configure(command, options, GetRemoveCommandOptions()));
            app.Command("show", command => ShowCommand.Configure(command, options, GetShowCommandOptions()));

            // action (for this command)
            app.OnExecute(() => app.ShowVersionAndHelp());
        }

        private static RemoveCommandOptions GetRemoveCommandOptions() =>
            new RemoveCommandOptions
            {
                Type = "identity provider",
                ArgumentName = "name",
                ArgumentDescription = "The name of the identity provider to remove",
                RemoveCommand = value => new RemoveCommand(async context => await context.IdentityProvidersClient.RemoveIdentityProviderAsync(value).ConfigureAwait(false)),
            };

        private static ShowCommandOptions GetShowCommandOptions() =>
            new ShowCommandOptions
            {
                Type = "identity provider",
                ArgumentName = "name",
                ArgumentDescription = "The identity provider name. You can end the identity provider name with a wildcard to search",
                DisplayCommand = (string value) =>
                    new ShowCommand.Display<IdentityProvider>(async context => await context.IdentityProvidersClient.GetIdentityProviderAsync(value).ConfigureAwait(false)),
                ListCommand = (string startsWith, int skip, int take) =>
                    new ShowCommand.List<IdentityProviderSummary>(
                        "identity providers",
                        async context => await context.IdentityProvidersClient.GetIdentityProviderSummariesAsync(startsWith, skip, take).ConfigureAwait(false),
                        ("name", resource => resource.Name),
                        ("description", resource => resource.DisplayName),
                        ("authority", resource => resource.Authority),
                        ("client", resource => resource.ClientId),
                        ("enabled", resource => resource.Enabled.ToString(CultureInfo.InvariantCulture))),
            };
    }
}