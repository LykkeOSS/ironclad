// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Console.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using McMaster.Extensions.CommandLineUtils;
    using Newtonsoft.Json;
    using IroncladClient = Ironclad.Client.Client;

    internal class AddClientCommand : ICommand
    {
        private IroncladClient client;

        private AddClientCommand()
        {
        }

        public static void Configure(CommandLineApplication app, CommandLineOptions options, IConsole console)
        {
            // description
            app.Description = "Creates a new client";
            app.ExtendedHelpText = $"{Environment.NewLine}Use 'clients add -i' to enter interactive mode{Environment.NewLine}";

            // arguments
            var argumentType = app.Argument("type", "The type of client to add (allowed values are s[erver], w[ebsite], and c[onsole])", false);
            var argumentClientId = app.Argument("id", "The client identifier", false);

            // options
#pragma warning disable SA1025
            var optionName =                        app.Option("-n|--name <name>",                 "The name of the client",                                                  CommandOptionType.SingleValue);
            var optionSecret =                      app.Option("-s|--secret <secret>",             "The client secret",                                                       CommandOptionType.SingleValue);
            var optionAllowedCorsOrigins =          app.Option("-c|--cors_uri <uri>",              "An allowed CORS origin for the client (you can call this several times)", CommandOptionType.MultipleValue);
            var optionRedirectUris =                app.Option("-r|--redirect_uri <uri>",          "A redirect URI for the client (you can call this several times)",         CommandOptionType.MultipleValue);
            var optionPostLogoutRedirectUris =      app.Option("-l|--logout_uri <uri>",            "A logout URI for the client (you can call this several times)",           CommandOptionType.MultipleValue);
            var optionAllowedScopes =               app.Option("-a|--scope <scope>",               "An allowed scope for the client (you can call this several times)",       CommandOptionType.MultipleValue);
            var optionAccessTokenType =             app.Option("-t|--token_type <Jwt/Reference>",  "The access token type for the client",                                    CommandOptionType.SingleValue);
            var optionAllowedGrantTypes =           app.Option("-g|--grant_type <type>",           "A grant type for the client (you can call this several times)",           CommandOptionType.MultipleValue);
            var optionAllowAccessTokensViaBrowser = app.Option("-b|--browser",                     "Allow access tokens via browser",                                         CommandOptionType.NoValue);
            var optionAllowOfflineAccess =          app.Option("-o|--offline",                     "Allow offline access",                                                    CommandOptionType.NoValue);
            var optionDoNotRequireClientSecret =    app.Option("-k|--no_secret",                   "Do not require client secret",                                            CommandOptionType.NoValue);
            var optionRequirePkce =                 app.Option("-p|--pkce",                        "Require Proof Key for Code Exchange (PKCE)",                              CommandOptionType.NoValue);
            var optionDoNotRequireConsent =         app.Option("-q|--no_constent",                 "Do not require consent",                                                  CommandOptionType.NoValue);
            var optionDisabled =                    app.Option("-d|--disabled",                    "Creates the new client in a disabled state",                              CommandOptionType.NoValue);
            var optionInteractive =                 app.Option("-i|--interactive",                 "Enters interactive mode",                                                 CommandOptionType.NoValue);
            var optionDisableLocalLogin =           app.Option("-e|--disable_local_login",         "Disable local login",                                                     CommandOptionType.NoValue);
#pragma warning restore SA1025

            app.HelpOption();

            // action (for this command)
            app.OnExecute(
                () =>
                {
                    var reporter = new ConsoleReporter(console, options.Verbose.HasValue(), false);

                    if (string.IsNullOrEmpty(argumentType.Value))
                    {
                        // TODO (Cameron): Prompt for client type.
                        reporter.Warn("The type of client is required. Allowed values are s[erver], w[ebsite], and c[onsole].");
                        return;
                    }

                    var helper = default(IHelper<IroncladClient>);
                    switch (argumentType.Value?.ToUpperInvariant())
                    {
                        case "S":
                        case "SERVER":
                            helper = new ServerClientHelper();
                            break;

                        case "W":
                        case "WEBSITE":
                            helper = new WebsiteClientHelper();
                            break;

                        case "C":
                        case "CONSOLE":
                            helper = new ConsoleClientHelper();
                            break;

                        case null:
                        default:
                            if (!optionInteractive.HasValue())
                            {
                                app.ShowVersionAndHelp();
                                return;
                            }
                            break;
                    }

#pragma warning disable CA1308
                    reporter.Verbose(
                        $"Command type configured for '{helper.GetType().Name.ToLowerInvariant().Replace("ClientHelper", string.Empty, StringComparison.OrdinalIgnoreCase)}'.");
#pragma warning restore CA1308

                    var client = helper.GetPrototype(
                        new IroncladClient
                        {
                            Id = argumentClientId.Value,
                            Secret = optionSecret.Value(),
                            Name = optionName.Value(),
                            AccessTokenType = optionAccessTokenType.Value(),
                            AllowedCorsOrigins = optionAllowedCorsOrigins.HasValue() ? optionAllowedCorsOrigins.Values.Distinct().ToHashSet() : null,
                            RedirectUris = optionRedirectUris.HasValue() ? optionRedirectUris.Values.Distinct().ToHashSet() : null,
                            PostLogoutRedirectUris = optionPostLogoutRedirectUris.HasValue() ? optionPostLogoutRedirectUris.Values.Distinct().ToHashSet() : null,
                            AllowedScopes = optionAllowedScopes.HasValue() ? optionAllowedScopes.Values.Distinct().ToHashSet() : null,
                            AllowedGrantTypes = optionAllowedGrantTypes.HasValue() ? optionAllowedGrantTypes.Values.Distinct().ToHashSet() : null,
                            AllowAccessTokensViaBrowser = optionAllowAccessTokensViaBrowser.HasValue() ? (bool?)true : null,
                            AllowOfflineAccess = optionAllowOfflineAccess.HasValue() ? (bool?)true : null,
                            RequirePkce = optionRequirePkce.HasValue() ? (bool?)true : null,
                            RequireClientSecret = optionDoNotRequireClientSecret.HasValue() ? (bool?)false : null,
                            RequireConsent = optionDoNotRequireConsent.HasValue() ? (bool?)false : null,
                            Enabled = optionDisabled.HasValue() ? (bool?)false : null,
                            EnableLocalLogin = optionDisableLocalLogin.HasValue() ? (bool?)false : null,
                        });

                    reporter.Verbose("Prototype client (from command line arguments):");
                    reporter.Verbose(JsonConvert.SerializeObject(client));

                    if (!helper.IsValid(client) || optionInteractive.HasValue())
                    {
                        try
                        {
                            client = helper.GetValid(client);
                        }
                        catch (NotSupportedException ex)
                        {
                            throw new CommandParsingException(app, $"Operation Aborted. {ex.Message}", ex);
                        }

                        reporter.Verbose("Validated client (from interactive console):");
                        reporter.Verbose(JsonConvert.SerializeObject(client));
                    }

                    options.Command = new AddClientCommand { client = client };
                });
        }

        public Task ExecuteAsync(CommandContext context) => context.ClientsClient.AddClientAsync(this.client);

        private static string Safe(string value, string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new NotSupportedException(errorMessage);
            }

            return value;
        }

        private class ServerClientHelper : IHelper<IroncladClient>
        {
            public IroncladClient GetPrototype(IroncladClient client)
            {
                client.AllowedGrantTypes = client.AllowedGrantTypes ?? new HashSet<string> { "client_credentials" };
                return client;
            }

            public bool IsValid(IroncladClient client) =>
                !string.IsNullOrEmpty(client.Id) &&
                !string.IsNullOrEmpty(client.Secret) &&
                client.AllowedScopes?.Any() == true &&
                client.AllowedGrantTypes.Contains("client_credentials");

            public IroncladClient GetValid(IroncladClient client)
            {
                client.Id = Safe(Prompt.GetString("Client identifier:", client.Id), "Cannot create a server client without a client identifier.");
                client.Name = Prompt.GetString("Client name:", client.Name);
                client.Secret = client.Secret ?? Safe(Prompt.GetPassword("Client secret:"), "Cannot create a server client without a client secret.");
                client.AllowedScopes = Safe(
                    Prompt.GetString(
                        "Allowed scopes for the client (space separated):",
                        client.AllowedScopes == null ? null : string.Join(' ', client.AllowedScopes)),
                    "Cannot create a server client without any allowed scopes.")
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                client.RequireConsent = Prompt.GetYesNo("Require consent?", true);

                // defaults
                client.Name = string.IsNullOrWhiteSpace(client.Name) ? null : client.Name;
                if (!client.AllowedGrantTypes.Contains("client_credentials"))
                {
                    client.AllowedGrantTypes.Add("client_credentials");
                }

                return client;
            }
        }

        private class WebsiteClientHelper : IHelper<IroncladClient>
        {
            public IroncladClient GetPrototype(IroncladClient client)
            {
                client.AllowAccessTokensViaBrowser = client.AllowAccessTokensViaBrowser ?? true;
                client.AllowedGrantTypes = client.AllowedGrantTypes ?? new HashSet<string> { "implicit" };
                return client;
            }

            public bool IsValid(IroncladClient client) =>
                !string.IsNullOrEmpty(client.Id) &&
                client.AllowedCorsOrigins?.Any() == true &&
                client.RedirectUris?.Any() == true &&
                client.AllowedScopes?.Any() == true &&
                client.AllowAccessTokensViaBrowser == true &&
                client.AllowedGrantTypes.Contains("implicit");

            public IroncladClient GetValid(IroncladClient client)
            {
                client.Id = Safe(Prompt.GetString("Client identifier:", client.Id), "Cannot create a website client without a client identifier.");
                client.Name = Prompt.GetString("Client name:", client.Name);
                client.AllowedCorsOrigins = Safe(
                    Prompt.GetString(
                        "Allowed CORS origins for the client (space separated):",
                        client.AllowedCorsOrigins == null ? null : string.Join(' ', client.AllowedCorsOrigins)),
                    "Cannot create a website client without any allowed CORS origins.")
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                client.RedirectUris = Safe(
                    Prompt.GetString(
                        "Redirect URIs for the client (space separated):",
                        client.RedirectUris == null ? null : string.Join(' ', client.RedirectUris)),
                    "Cannot create a website client without any redirect URIs.")
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                client.PostLogoutRedirectUris = Prompt.GetString(
                    "Allowed post-logout redirect URIs for the client (space separated) [optional]:",
                    client.PostLogoutRedirectUris == null ? null : string.Join(' ', client.PostLogoutRedirectUris))
                    ?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                client.AllowedScopes = Safe(
                    Prompt.GetString(
                        "Allowed scopes for the client (space separated):",
                        client.AllowedScopes == null ? null : string.Join(' ', client.AllowedScopes)),
                    "Cannot create a website client without any allowed scopes.")
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                client.RequireConsent = Prompt.GetYesNo("Require consent?", client.RequireConsent ?? true);

                // defaults
                client.Name = string.IsNullOrWhiteSpace(client.Name) ? null : client.Name;
                client.AllowAccessTokensViaBrowser = true;
                if (!client.AllowedGrantTypes.Contains("implicit"))
                {
                    client.AllowedGrantTypes.Add("implicit");
                }

                return client;
            }
        }

        private class ConsoleClientHelper : IHelper<IroncladClient>
        {
            public IroncladClient GetPrototype(IroncladClient client)
            {
                client.RedirectUris = client.RedirectUris ?? new HashSet<string> { "http://127.0.0.1" };
                client.AllowedGrantTypes = client.AllowedGrantTypes ?? new HashSet<string> { "hybrid" };
                client.AllowOfflineAccess = client.AllowOfflineAccess ?? true;
                client.RequireClientSecret = client.RequireClientSecret ?? false;
                client.RequirePkce = client.RequirePkce ?? true;
                client.AccessTokenType = client.AccessTokenType ?? "Reference";
                return client;
            }

            public bool IsValid(IroncladClient client) =>
                !string.IsNullOrEmpty(client.Id) &&
                client.AllowedScopes?.Any() == true &&
                client.RedirectUris?.Any() == true &&
                client.AllowOfflineAccess == true &&
                client.RequireClientSecret == false &&
                client.RequirePkce == true &&
                client.AllowedGrantTypes.Contains("hybrid");

            public IroncladClient GetValid(IroncladClient client)
            {
                client.Id = Safe(Prompt.GetString("Client identifier:", client.Id), "Cannot create a console client without a client identifier.");
                client.Name = Prompt.GetString("Client name:", client.Name);
                client.AllowedScopes = Safe(
                    Prompt.GetString(
                        "Allowed scopes for the client (space separated):",
                        client.AllowedScopes == null ? null : string.Join(' ', client.AllowedScopes)),
                    "Cannot create a console client without any allowed scopes.")
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                client.AllowOfflineAccess = Prompt.GetYesNo("Allow offline access?", true);
                client.RequireConsent = Prompt.GetYesNo("Require consent?", true);
                client.EnableLocalLogin = Prompt.GetYesNo("Enable local login? (default: true)", true);

                // defaults
                client.Name = string.IsNullOrWhiteSpace(client.Name) ? null : client.Name;
                client.RequireClientSecret = false;
                client.RequirePkce = true;
                if (!client.RedirectUris.Contains("http://127.0.0.1"))
                {
                    client.RedirectUris.Add("http://127.0.0.1");
                }

                if (!client.AllowedGrantTypes.Contains("hybrid"))
                {
                    client.AllowedGrantTypes.Add("hybrid");
                }

                return client;
            }
        }
    }
}