// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Console.Commands
{
    using Ironclad.Client;
    using Ironclad.Console.Persistence;
    using McMaster.Extensions.CommandLineUtils;

    public class CommandContext
    {
        public CommandContext(
            IConsole console,
            IReporter reporter,
            IClientsClient clientsClient,
            IApiResourcesClient apiResourcesClient,
            IIdentityResourcesClient identityResourcesClient,
            IRolesClient rolesClient,
            IUsersClient usersClient,
            IIdentityProvidersClient identityProvidersClient,
            ICommandDataRepository repository)
        {
            this.Console = console;
            this.Reporter = reporter;
            this.ClientsClient = clientsClient;
            this.ApiResourcesClient = apiResourcesClient;
            this.IdentityResourcesClient = identityResourcesClient;
            this.RolesClient = rolesClient;
            this.UsersClient = usersClient;
            this.IdentityProvidersClient = identityProvidersClient;
            this.Repository = repository;
        }

        public IConsole Console { get; }

        public IReporter Reporter { get; }

        public IClientsClient ClientsClient { get; }

        public IApiResourcesClient ApiResourcesClient { get; }

        public IIdentityResourcesClient IdentityResourcesClient { get; }

        public IRolesClient RolesClient { get; }

        public IUsersClient UsersClient { get; }

        public IIdentityProvidersClient IdentityProvidersClient { get; }

        public ICommandDataRepository Repository { get; }
    }
}