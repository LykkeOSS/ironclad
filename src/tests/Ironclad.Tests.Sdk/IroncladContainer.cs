// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Sdk
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

    internal class IroncladContainer : Container
    {
        private readonly IroncladProbe probe;

        public IroncladContainer(string authority, int port, string connectionString, string dockerRegistry, NetworkCredential dockerCredentials, string dockerTag, bool outputDockerLogs)
        {
            authority = authority ?? throw new ArgumentNullException(nameof(authority));
            connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));

            this.Configuration = new ContainerConfiguration
            {
                Registry = dockerRegistry,
                RegistryCredentials = dockerCredentials,
                Image = "ironclad",
                Tag = dockerTag,
                ContainerName = "ironclad-integration",
                ContainerPortBindings = new[]
                {
                    new ContainerConfiguration.PortBinding
                    {
                        GuestTcpPort = 80, HostTcpPort = port
                    }
                },
                ContainerEnvironmentVariables = new[]
                {
                    $"ASPNETCORE_URLS=http://*:80",

                    // LINK (Cameron): https://gist.github.com/cameronfletcher/58673a468c8ebbbf91b81e706063ba56
                    $"SERVER__DATABASE={connectionString}",
                    $"API__AUTHORITY=http://localhost",
                    $"API__CLIENT_ID=auth_api",
                    $"API__SECRET=secret", // self-introspection secret (not a secret)
                },
                OutputDockerLogs = outputDockerLogs
            };

            this.probe = new IroncladProbe(authority, 2, 20);
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(false);
            await this.probe.WaitUntilAvailable(true, default).ConfigureAwait(false);
        }
    }
}