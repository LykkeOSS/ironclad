using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Npgsql;

namespace Ironclad.Tests.Sdk
{
    internal static class WellKnownContainers
    {
        private static int PostgresContainerNameSuffix;

        public static LocalDockerContainer CreatePostgres(NpgsqlConnectionStringBuilder builder) =>
            new LocalDockerContainer(
                new LocalDockerContainerConfiguration
                {
                    Image = "postgres", Tag = "10.1-alpine",
                    ContainerName = "ironclad-postgres" + Interlocked.Increment(ref PostgresContainerNameSuffix),
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
                            using (var connection = new NpgsqlConnection(builder.ConnectionString))
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
                    MaximumWaitUntilAvailableAttempts = 20,
                    TimeBetweenWaitUntilAvailableAttempts = TimeSpan.FromMilliseconds(100)
                });
    }
}