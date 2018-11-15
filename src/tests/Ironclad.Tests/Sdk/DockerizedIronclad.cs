// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Sdk
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using Npgsql;

    public class DockerizedIronclad : LocalDockerContainer
    {
        private static long ironcladContainerNameSuffix = DateTime.UtcNow.Ticks;

        public DockerizedIronclad(string authority, string connectionString)
        {
            if (authority == null)
            {
                throw new ArgumentNullException(nameof(authority));
            }

            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            this.Configuration = new LocalDockerContainerConfiguration
            {
                Image = "ironclad",
                Tag = "dev",
                ContainerName = "ironclad" + Interlocked.Increment(ref ironcladContainerNameSuffix),
                AutoRemoveContainer = true,
                ContainerPortBindings = new[]
                {
                    new LocalDockerContainerPortBinding
                    {
                        GuestTcpPort = 80, HostTcpPort = 5005
                    }
                },
                ContainerEnvironmentVariables = new[]
                {
                    $"IRONCLAD_CONNECTIONSTRING={connectionString}"
                },
                WaitUntilAvailable = async token =>
                {
                    using (var client = new HttpClient())
                    {
                        try
                        {
                            using (var response = await client
                                .GetAsync(new Uri(authority + "/api"), token)
                                .ConfigureAwait(false))
                            {
                                if (response.StatusCode == HttpStatusCode.OK)
                                {
                                    return true;
                                }
                            }
                        }
                        catch (HttpRequestException)
                        {
                        }
                    }

                    return false;
                },
                MaximumWaitUntilAvailableAttempts = 15,
                TimeBetweenWaitUntilAvailableAttempts = TimeSpan.FromSeconds(1)
            };
        }
    }
}