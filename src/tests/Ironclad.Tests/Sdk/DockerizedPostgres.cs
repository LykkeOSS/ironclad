// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Sdk
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading;
    using Npgsql;

    public class DockerizedPostgres : LocalDockerContainer
    {
        private static long postgresContainerNameSuffix = DateTime.UtcNow.Ticks;

        public DockerizedPostgres(NpgsqlConnectionStringBuilder connectionStringBuilder)
        {
            if (connectionStringBuilder == null)
            {
                throw new ArgumentNullException(nameof(connectionStringBuilder));
            }

            this.Configuration = new LocalDockerContainerConfiguration
            {
                Image = "postgres", Tag = "10.1-alpine",
                ContainerName = "ironclad-postgres" + Interlocked.Increment(ref postgresContainerNameSuffix),
                ContainerPortBindings = new[]
                {
                    new LocalDockerContainerPortBinding
                    {
                        GuestTcpPort = connectionStringBuilder.Port, HostTcpPort = 5432
                    }
                },
                ContainerEnvironmentVariables = new[]
                {
                    "POSTGRES_PASSWORD=" + connectionStringBuilder.Password,
                    "POSTGRES_DB=" + connectionStringBuilder.Database
                },
                AutoRemoveContainer = true,
                WaitUntilAvailable = async token =>
                {
                    try
                    {
                        using (var connection = new NpgsqlConnection(connectionStringBuilder.ConnectionString))
                        {
                            await connection.OpenAsync(token).ConfigureAwait(false);
                        }

                        return true;
                    }
                    catch (Exception exception) when (exception is NpgsqlException
                                                      || exception is SocketException
                                                      || exception is EndOfStreamException)
                    {
                    }

                    return false;
                },
                MaximumWaitUntilAvailableAttempts = 10,
                TimeBetweenWaitUntilAvailableAttempts = TimeSpan.FromSeconds(1)
            };
        }
    }
}