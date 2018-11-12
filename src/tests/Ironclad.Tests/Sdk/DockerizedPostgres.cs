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
        private static int PostgresContainerNameSuffix;
        
        private static readonly string ConnectionString =
            "Host=localhost;Database=ironclad;Username=postgres;Password=postgres;";

        private readonly NpgsqlConnectionStringBuilder _builder; 
        
        public DockerizedPostgres()
        {
            _builder = new NpgsqlConnectionStringBuilder(ConnectionString);
            
            Configuration = new LocalDockerContainerConfiguration
            {
                Image = "postgres", Tag = "10.1-alpine",
                ContainerName = "ironclad-postgres" + Interlocked.Increment(ref PostgresContainerNameSuffix),
                ContainerPortBindings = new[]
                {
                    new LocalDockerContainerPortBinding
                    {
                        GuestTcpPort = _builder.Port, HostTcpPort = 5432
                    }
                },
                ContainerEnvironmentVariables = new[]
                {
                    "POSTGRES_PASSWORD=" + _builder.Password,
                    "POSTGRES_DB=" + _builder.Database
                },
                AutoRemoveContainer = true,
                WaitUntilAvailable = async token =>
                {
                    try
                    {
                        using (var connection = new NpgsqlConnection(_builder.ConnectionString))
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

        public NpgsqlConnectionStringBuilder ConnectionStringBuilder => _builder;
    }
}