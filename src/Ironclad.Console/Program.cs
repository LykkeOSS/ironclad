// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

#pragma warning disable CA1812

namespace Ironclad.Console
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using IdentityModel.Client;
    using Ironclad.Client;
    using Ironclad.Console.Commands;
    using Ironclad.Console.Persistence;
    using McMaster.Extensions.CommandLineUtils;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    internal class Program
    {
        private readonly IConsole console;
        private readonly IDataProtectionProvider provider;

        public Program(IConsole console, IDataProtectionProvider provider)
        {
            this.console = console;
            this.provider = provider;
        }

        public static Task<int> Main(string[] args)
        {
            Sdk.DebugHelper.HandleDebugSwitch(ref args);

            JsonConvert.DefaultSettings =
                () =>
                new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
                };

            // LINK (Cameron): https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/using-data-protection
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDataProtection();
            serviceCollection.AddSingleton(PhysicalConsole.Singleton);
            var services = serviceCollection.BuildServiceProvider();

            var instance = ActivatorUtilities.CreateInstance<Program>(services);
            return instance.TryRunAsync(args);
        }

        public async Task<int> TryRunAsync(string[] args)
        {
            CommandLineOptions options;
            try
            {
                options = CommandLineOptions.Parse(args, this.console);
            }
            catch (CommandParsingException ex)
            {
                new ConsoleReporter(this.console).Warn(ex.Message);
                return 1;
            }

            if (options == null)
            {
                return 1;
            }

            if (options.Help.HasValue())
            {
                return 2;
            }

            if (options.Command == null)
            {
                return 3;
            }

            var repository = new CommandDataRepository(this.provider);

            if (options.Command is LoginCommand.Reset)
            {
                await options.Command.ExecuteAsync(new CommandContext(this.console, null, null, null, null, null, null, null, repository)).ConfigureAwait(false);
                return 0;
            }

            var data = repository.GetCommandData() ??
                new CommandData
                {
                    Authority = LoginCommand.ProductionAuthority,
                };

            var authority = data.Authority;
            if (options.Command is LoginCommand loginCommand)
            {
                authority = loginCommand.Authority;
            }
            else
            {
                this.console.Write("Executing command against ");
                this.console.ForegroundColor = ConsoleColor.White;
                this.console.Write(authority);
                this.console.ResetColor();
                this.console.WriteLine("...");
            }

            var discoveryResponse = default(DiscoveryResponse);
            using (var discoveryClient = new DiscoveryClient(authority) { Policy = new DiscoveryPolicy { ValidateIssuerName = false } })
            {
                discoveryResponse = await discoveryClient.GetAsync().ConfigureAwait(false);
                if (discoveryResponse.IsError)
                {
                    await this.console.Error.WriteLineAsync(discoveryResponse.Error).ConfigureAwait(false);
                    return 500;
                }
            }

            using (var tokenClient = new TokenClient(discoveryResponse.TokenEndpoint, "auth_console"))
            using (var refreshTokenHandler = new RefreshTokenDelegatingHandler(tokenClient, data.RefreshToken, data.AccessToken) { InnerHandler = new HttpClientHandler() })
            using (var clientsClient = new ClientsHttpClient(authority, refreshTokenHandler))
            using (var apiResourcesClient = new ApiResourcesHttpClient(authority, refreshTokenHandler))
            using (var identityResourcesClient = new IdentityResourcesHttpClient(authority, refreshTokenHandler))
            using (var rolesClient = new RolesHttpClient(authority, refreshTokenHandler))
            using (var usersClient = new UsersHttpClient(authority, refreshTokenHandler))
            using (var identityProvidersClient = new IdentityProvidersHttpClient(authority, refreshTokenHandler))
            {
                refreshTokenHandler.TokenRefreshed += (sender, e) =>
                {
                    repository.SetCommandData(
                        new CommandData
                        {
                            Authority = authority,
                            AccessToken = e.AccessToken,
                            RefreshToken = e.RefreshToken,
                        });
                };

                var reporter = new ConsoleReporter(this.console, options.Verbose.HasValue(), false);
                var context = new CommandContext(
                    this.console,
                    reporter,
                    clientsClient,
                    apiResourcesClient,
                    identityResourcesClient,
                    rolesClient,
                    usersClient,
                    identityProvidersClient,
                    repository);

                try
                {
                    await options.Command.ExecuteAsync(context).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    reporter.Error(ex.Message);
                    return 500;
                }
                finally
                {
                    this.console.ResetColor();
                }

                return 0;
            }
        }
    }
}
