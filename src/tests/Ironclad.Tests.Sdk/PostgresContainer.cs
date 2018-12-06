// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Sdk
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Npgsql;

    internal class PostgresContainer : Container
    {
        private readonly PostgresProbe probe;

        public PostgresContainer(NpgsqlConnectionStringBuilder connectionStringBuilder)
        {
            if (connectionStringBuilder == null)
            {
                throw new ArgumentNullException(nameof(connectionStringBuilder));
            }

            this.Configuration = new ContainerConfiguration
            {
                Image = "postgres", Tag = "10.1-alpine",
                IsContainerReusable = false,
                ContainerName = "ironclad-integration-postgres",
                ContainerPortBindings = new[]
                {
                    new ContainerConfiguration.PortBinding
                    {
                        GuestTcpPort = connectionStringBuilder.Port, HostTcpPort = 5432
                    }
                },
                ContainerEnvironmentVariables = new[]
                {
                    "POSTGRES_PASSWORD=" + connectionStringBuilder.Password,
                    "POSTGRES_DB=" + connectionStringBuilder.Database
                },
            };

            this.probe = new PostgresProbe(connectionStringBuilder.ConnectionString, 4, 20);
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(false);
            await this.probe.WaitUntilAvailable(true, default).ConfigureAwait(false);
        }
    }
}