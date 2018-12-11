// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Console.Commands
{
    using System;
    using System.Threading.Tasks;
    using Ironclad.Client;
    using McMaster.Extensions.CommandLineUtils;
    using Newtonsoft.Json;

    public class AddIdentityProviderCommand : ICommand
    {
        private IdentityProvider identityProvider;

        private AddIdentityProviderCommand()
        {
        }

        public static void Configure(CommandLineApplication app, CommandLineOptions options, IConsole console)
        {
            // description
            app.Description = $"Creates a new identity provider";
            app.ExtendedHelpText = $"{Environment.NewLine}Use 'providers add -i' to enter interactive mode{Environment.NewLine}";

            // arguments
            var argumentName = app.Argument("name", "The identity provider name", false);
            var argumentAuthority = app.Argument("authority", "The identity provider authority", false);
            var argumentClientId = app.Argument("id", "The identity provider client identifier", false);

            // options
#pragma warning disable SA1025
            var optionDisplayName =  app.Option("-d|--description <description>",    "The identity provider description",            CommandOptionType.SingleValue);
            var optionCallbackPath = app.Option("-c|--callback <path>",              "The callback path for the identity provider",  CommandOptionType.SingleValue);
            var optionAcrValues =    app.Option("-a|--acr <value>",                  "The acr_values for the identity provider (you can call this several times). Example: (\"tenant:ironclad\").",  CommandOptionType.MultipleValue);
            var optionInteractive =  app.Option("-i|--interactive",                  "Enters interactive mode",                      CommandOptionType.NoValue);
#pragma warning restore SA1025

            app.HelpOption();

            // action (for this command)
            app.OnExecute(
                () =>
                {
                    if ((string.IsNullOrEmpty(argumentName.Value) || string.IsNullOrEmpty(argumentAuthority.Value) || string.IsNullOrEmpty(argumentClientId.Value))
                        && !optionInteractive.HasValue())
                    {
                        app.ShowVersionAndHelp();
                        return;
                    }

                    var reporter = new ConsoleReporter(console, options.Verbose.HasValue(), false);
                    var helper = new IdentityProviderHelper();

                    var resource = new IdentityProvider
                    {
                        Name = argumentName.Value,
                        DisplayName = optionDisplayName.Value(),
                        Authority = argumentAuthority.Value,
                        ClientId = argumentClientId.Value,
                        CallbackPath = optionCallbackPath.Value(),
                        AcrValues = optionAcrValues.Value()
                    };

                    reporter.Verbose("Prototype identity provider (from command line arguments):");
                    reporter.Verbose(JsonConvert.SerializeObject(resource));

                    if (!helper.IsValid(resource) || optionInteractive.HasValue())
                    {
                        try
                        {
                            resource = helper.GetValid(resource);
                        }
                        catch (NotSupportedException ex)
                        {
                            throw new CommandParsingException(app, $"Operation Aborted. {ex.Message}", ex);
                        }

                        reporter.Verbose("Validated identity provider (from interactive console):");
                        reporter.Verbose(JsonConvert.SerializeObject(resource));
                    }

                    options.Command = new AddIdentityProviderCommand { identityProvider = resource };
                });
        }

        public async Task ExecuteAsync(CommandContext context) => await context.IdentityProvidersClient.AddIdentityProviderAsync(this.identityProvider).ConfigureAwait(false);

        private static string Safe(string value, string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new NotSupportedException(errorMessage);
            }

            return value;
        }

        private class IdentityProviderHelper : IHelper<IdentityProvider>
        {
            public IdentityProvider GetPrototype(IdentityProvider identityProvider) => identityProvider;

            public bool IsValid(IdentityProvider identityProvider) =>
                !string.IsNullOrEmpty(identityProvider.Name) &&
                !string.IsNullOrEmpty(identityProvider.Authority) &&
                !string.IsNullOrEmpty(identityProvider.ClientId);

            public IdentityProvider GetValid(IdentityProvider identityProvider)
            {
                identityProvider.Name = Safe(Prompt.GetString("Provider name:", identityProvider.Name), "Cannot create an identity provider without a name.");
                identityProvider.DisplayName = Prompt.GetString("Provider description:", identityProvider.DisplayName);
                identityProvider.Authority = identityProvider.Authority ?? Safe(Prompt.GetString("Authority:"), "Cannot create an identity provider with no authority.");
                identityProvider.ClientId = identityProvider.ClientId ??
                    Safe(Prompt.GetString("Client identifier:"), "Cannot create an identity provider without a client identifier.");
                identityProvider.CallbackPath = Prompt.GetString("Callback path for the identity provider [optional]:", identityProvider.CallbackPath);
                identityProvider.AcrValues = Prompt.GetString("AcrValues for the identity provider(Example \"tenant:ironclad idp:ironclad\"). [optional]:", identityProvider.AcrValues);

                // defaults
                identityProvider.DisplayName = string.IsNullOrWhiteSpace(identityProvider.DisplayName) ? null : identityProvider.DisplayName;
                identityProvider.CallbackPath = string.IsNullOrWhiteSpace(identityProvider.CallbackPath) ? null : identityProvider.CallbackPath;

                return identityProvider;
            }
        }
    }
}
