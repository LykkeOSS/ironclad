// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

#pragma warning disable CA1001

namespace Ironclad.Tests.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Docker.DotNet;
    using Docker.DotNet.Models;
    using Xunit;

    internal abstract class Container : IAsyncLifetime
    {
        private const string UnixPipe = "unix:///var/run/docker.sock";
        private const string WindowsPipe = "npipe://./pipe/docker_engine";

        private readonly DockerClientConfiguration clientConfiguration =
            new DockerClientConfiguration(
                new Uri(Environment.GetEnvironmentVariable("DOCKER_HOST") ?? (Environment.OSVersion.Platform.Equals(PlatformID.Unix) ? UnixPipe : WindowsPipe)));

        private readonly DockerClient client;
        private ContainerConfiguration configuration;

        protected Container()
        {
            this.client = this.clientConfiguration.CreateClient();
        }

        protected ContainerConfiguration Configuration
        {
            get => this.configuration;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (string.IsNullOrEmpty(value.Image))
                {
                    throw new ArgumentException("Please specify the Image the container is based on.", nameof(value));
                }

                if (string.IsNullOrEmpty(value.Tag))
                {
                    throw new ArgumentException("Please specify the Tag of the Image the container is based on.", nameof(value));
                }

                if (string.IsNullOrEmpty(value.ContainerName))
                {
                    throw new ArgumentException("Please specify the ContainerName of the container.", nameof(value));
                }

                if (value.ContainerPortBindings == null)
                {
                    throw new ArgumentException("Please specify either an empty or filled list of ContainerPortBindings for the container.", nameof(value));
                }

                if (value.ContainerEnvironmentVariables == null)
                {
                    throw new ArgumentException("Please specify either an empty or filled list of ContainerEnvironmentVariables for the container.", nameof(value));
                }

                this.configuration = value;
            }
        }

        public virtual async Task InitializeAsync()
        {
            if (this.Configuration == null)
            {
                throw new InvalidOperationException("Please provide the Configuration before initializing the fixture.");
            }

            var id = await this.TryFindContainer(default).ConfigureAwait(false);
            if (id == null)
            {
                await this.AutoCreateImage(default).ConfigureAwait(false);
                id = await this.CreateContainer(default).ConfigureAwait(false);
            }
            else
            {
                await this.StopContainer(id, default).ConfigureAwait(false);
                await this.RemoveContainer(id, default).ConfigureAwait(false);
                id = await this.CreateContainer(default).ConfigureAwait(false);
            }

            await this.StartContainer(id, default).ConfigureAwait(false);
        }

        public virtual async Task DisposeAsync()
        {
            if (this.client != null && this.Configuration != null)
            {
                var id = await this.TryFindContainer(default).ConfigureAwait(false);
                if (id != null)
                {
                    if (this.Configuration.OutputDockerLogs)
                    {
                        using (var stream = await this.client.Containers.GetContainerLogsAsync(id, new ContainerLogsParameters { Follow = false, ShowStderr = true, ShowStdout = true }).ConfigureAwait(false))
                        using (var reader = new StreamReader(stream))
                        {
                            var logs = await reader.ReadToEndAsync().ConfigureAwait(false);
                            Console.WriteLine(logs);
                        }
                    }

                    await this.StopContainer(id, default).ConfigureAwait(false);
                    await this.RemoveContainer(id, default).ConfigureAwait(false);
                }
            }

            this.client?.Dispose();
            this.clientConfiguration.Dispose();
        }

        private async Task<string> TryFindContainer(CancellationToken token)
        {
            var containers = await this.client.Containers.ListContainersAsync(
                new ContainersListParameters
                {
                    All = true,
                    Filters = new Dictionary<string, IDictionary<string, bool>>
                    {
                        ["name"] = new Dictionary<string, bool>
                        {
                            [this.Configuration.ContainerName] = true
                        }
                    }
                }, token).ConfigureAwait(false);

            return containers.FirstOrDefault(
                container =>
                    container.Names.Contains("/" + this.configuration.ContainerName, StringComparer.OrdinalIgnoreCase))?.ID;
        }

        private async Task<string> CreateContainer(CancellationToken token)
        {
            var portBindings = this.Configuration.ContainerPortBindings.ToDictionary(
                binding => $"{binding.GuestTcpPort}/tcp",
                binding => (IList<PortBinding>)new List<PortBinding>
                {
                    new PortBinding
                    {
                        HostPort = binding.HostTcpPort.ToString(CultureInfo.InvariantCulture)
                    }
                });

            var parameters = new CreateContainerParameters
            {
                Image = this.Configuration.FullyQualifiedImage,
                Name = this.Configuration.ContainerName,
                Tty = true,
                Env = this.Configuration.ContainerEnvironmentVariables,
                HostConfig = new HostConfig
                {
                    PortBindings = portBindings
                }
            };

            var container = await this.client.Containers.CreateContainerAsync(parameters, token).ConfigureAwait(false);

            return container.ID;
        }

        private async Task StartContainer(string id, CancellationToken token) =>
            await this.client.Containers.StartContainerAsync(id, new ContainerStartParameters(), token).ConfigureAwait(false);

        private async Task StopContainer(string id, CancellationToken token) =>
            await this.client.Containers
                .StopContainerAsync(id, new ContainerStopParameters { WaitBeforeKillSeconds = 5 }, token)
                .ConfigureAwait(false);

        private async Task RemoveContainer(string id, CancellationToken token) =>
            await this.client.Containers
                .RemoveContainerAsync(id, new ContainerRemoveParameters { Force = false }, token)
                .ConfigureAwait(false);

        private async Task AutoCreateImage(CancellationToken token)
        {
            if (await this.ImageExists(token).ConfigureAwait(false))
            {
                return;
            }

            var authConfig = this.configuration.RegistryCredentials == null
                ? null
                : new AuthConfig { Username = this.configuration.RegistryCredentials.UserName, Password = this.configuration.RegistryCredentials.Password };

            await this.client.Images
                .CreateImageAsync(
                    new ImagesCreateParameters
                    {
                        FromImage = this.Configuration.RegistryQualifiedImage,
                        Tag = this.Configuration.Tag
                    },
                    authConfig,
                    Progress.IsBeingIgnored,
                    token)
                .ConfigureAwait(false);
        }

        private async Task<bool> ImageExists(CancellationToken token)
        {
            var images = await this.client.Images.ListImagesAsync(new ImagesListParameters { MatchName = this.Configuration.FullyQualifiedImage }, token).ConfigureAwait(false);
            return images.Count != 0;
        }

        private class Progress : IProgress<JSONMessage>
        {
            public static readonly IProgress<JSONMessage> IsBeingIgnored = new Progress();

            public void Report(JSONMessage value)
            {
            }
        }
    }
}