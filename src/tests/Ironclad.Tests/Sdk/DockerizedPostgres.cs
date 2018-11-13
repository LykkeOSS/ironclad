// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Sdk
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading;
    using Npgsql;

    public class DockerizedPostgres : LocalDockerContainer, IPostgresFixture
    {
        private static long postgresContainerNameSuffix = DateTime.UtcNow.Ticks;
        
        private static readonly string ConnectionString =
            "Host=localhost;Database=ironclad;Username=postgres;Password=postgres;";

        private readonly NpgsqlConnectionStringBuilder builder; 
        
        public DockerizedPostgres()
        {
            this.builder = new NpgsqlConnectionStringBuilder(ConnectionString);
            
            this.Configuration = new LocalDockerContainerConfiguration
            {
                Image = "postgres", Tag = "10.1-alpine",
                ContainerName = "ironclad-postgres" + Interlocked.Increment(ref postgresContainerNameSuffix),
                ContainerPortBindings = new[]
                {
                    new LocalDockerContainerPortBinding
                    {
                        GuestTcpPort = builder.Port, HostTcpPort = 5432
                    }
                },
                ContainerEnvironmentVariables = new[]
                {
                    "POSTGRES_PASSWORD=" + builder.Password,
                    "POSTGRES_DB=" + builder.Database
                },
                AutoRemoveContainer = true,
                WaitUntilAvailable = async token =>
                {
                    try
                    {
                        using (var connection = new NpgsqlConnection(this.builder.ConnectionString))
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
                MaximumWaitUntilAvailableAttempts = 30,
                TimeBetweenWaitUntilAvailableAttempts = TimeSpan.FromSeconds(2)
            };
        }

        public NpgsqlConnectionStringBuilder ConnectionStringBuilder => builder;
    }
}