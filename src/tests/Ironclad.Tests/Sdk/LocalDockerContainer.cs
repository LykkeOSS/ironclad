// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Docker.DotNet;
    using Docker.DotNet.Models;
    using Xunit;

    // ReSharper disable once CA1001
    public abstract class LocalDockerContainer : IAsyncLifetime
    {
        private const string UnixPipe = "unix:///var/run/docker.sock";
        private const string WindowsPipe = "npipe://./pipe/docker_engine";

        private readonly DockerClientConfiguration clientConfiguration =
            new DockerClientConfiguration(
                new Uri(Environment.OSVersion.Platform.Equals(PlatformID.Unix) ? UnixPipe : WindowsPipe)
            );

        private readonly DockerClient client;
        private LocalDockerContainerConfiguration configuration;

        protected LocalDockerContainer()
        {
            this.client = this.clientConfiguration.CreateClient();
        }

        protected LocalDockerContainerConfiguration Configuration
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
                    throw new ArgumentException(
                        "Please specify the Image the container is based on.",
                        nameof(value));
                }

                if (string.IsNullOrEmpty(value.Tag))
                {
                    throw new ArgumentException(
                        "Please specify the Tag of the Image the container is based on.",
                        nameof(value));
                }

                if (string.IsNullOrEmpty(value.ContainerName))
                {
                    throw new ArgumentException(
                        "Please specify the ContainerName of the container.",
                        nameof(value));
                }

                if (value.ContainerPortBindings == null)
                {
                    throw new ArgumentException(
                        "Please specify either an empty or filled list of ContainerPortBindings for the container.",
                        nameof(value));
                }

                if (value.ContainerEnvironmentVariables == null)
                {
                    throw new ArgumentException(
                        "Please specify either an empty or filled list of ContainerEnvironmentVariables for the container.",
                        nameof(value));
                }

                if (value.WaitUntilAvailable == null)
                {
                    throw new ArgumentException(
                        "Please specify the WaitUntilAvailable action to execute on the container.",
                        nameof(value));
                }

                if (value.MaximumWaitUntilAvailableAttempts <= 0)
                {
                    throw new ArgumentException(
                        "Please specify a MaximumWaitUntilAvailableAttempts greater than or equal to 1.",
                        nameof(value));
                }

                if (value.TimeBetweenWaitUntilAvailableAttempts < TimeSpan.Zero)
                {
                    throw new ArgumentException(
                        "Please specify a TimeBetweenWaitUntilAvailableAttempts greater than or equal to TimeSpan.Zero.",
                        nameof(value));
                }

                this.configuration = value;
            }
        }

        public async Task InitializeAsync()
        {
            if (this.Configuration == null)
            {
                throw new InvalidOperationException("Please provide the Configuration before initializing the fixture.");
            }
            
            if (this.Configuration.IsContainerReusable)
            {
                var id = await TryFindContainer(default).ConfigureAwait(false);
                if (id == null)
                {
                    await AutoCreateImage(default).ConfigureAwait(false);
                    id = await CreateContainer(default).ConfigureAwait(false);
                }

                await StartContainer(id, default).ConfigureAwait(false);
            }
            else
            {
                await AutoCreateImage(default).ConfigureAwait(false);
                await AutoStartContainer(default).ConfigureAwait(false);
            }
        }

        public async Task DisposeAsync()
        {
            if (this.client != null && this.Configuration != null)
            {
                var id = await TryFindContainer(default).ConfigureAwait(false);
                if (id != null)
                {
                    await StopContainer(id, default).ConfigureAwait(false);
                    if (this.configuration.AutoRemoveContainer)
                    {
                        await RemoveContainer(id, default).ConfigureAwait(false);
                    }
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

            return containers
                .FirstOrDefault(container => container.State != "exited")
                ?.ID;
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
                Image = this.Configuration.TagQualifiedImage,
                Name = this.Configuration.ContainerName,
                Tty = true,
                Env = this.Configuration.ContainerEnvironmentVariables,
                HostConfig = new HostConfig
                {
                    PortBindings = portBindings
                }
            };

            var container = await this.client.Containers
                .CreateContainerAsync(parameters, token)
                .ConfigureAwait(false);

            return container.ID;
        }

        private async Task AutoStartContainer(CancellationToken token)
        {
            var id = await CreateContainer(token).ConfigureAwait(false);
            if (id != null)
            {
                await StartContainer(id, token).ConfigureAwait(false);
            }
        }

        private async Task StartContainer(string id, CancellationToken token)
        {
            var started = await this.client.Containers
                .StartContainerAsync(id, new ContainerStartParameters(), token)
                .ConfigureAwait(false);

            if (started)
            {
                var attempt = 0;
                while (
                    attempt < this.Configuration.MaximumWaitUntilAvailableAttempts &&
                    !await this.Configuration.WaitUntilAvailable(token).ConfigureAwait(false))
                {
                    if (attempt != this.Configuration.MaximumWaitUntilAvailableAttempts - 1)
                    {
                        await Task
                            .Delay(this.Configuration.TimeBetweenWaitUntilAvailableAttempts, token)
                            .ConfigureAwait(false);
                    }

                    attempt++;
                }

                if (attempt == this.Configuration.MaximumWaitUntilAvailableAttempts)
                {
                    throw new Exception(
                        $"The container {this.Configuration.ContainerName} did not become available in a timely fashion.");
                }
            }
        }

        private async Task StopContainer(string id, CancellationToken token)
        {
            await this.client.Containers
                .StopContainerAsync(id, new ContainerStopParameters {WaitBeforeKillSeconds = 5}, token)
                .ConfigureAwait(false);
        }

        private async Task RemoveContainer(string id, CancellationToken token)
        {
            await this.client.Containers
                .RemoveContainerAsync(id, new ContainerRemoveParameters {Force = false}, token)
                .ConfigureAwait(false);
        }

        private async Task AutoCreateImage(CancellationToken token)
        {
            if (!await this.ImageExists(token).ConfigureAwait(false))
            {
                await this.client
                    .Images
                    .CreateImageAsync(
                        new ImagesCreateParameters
                        {
                            FromImage = this.Configuration.Image,
                            Tag = this.Configuration.Tag
                        },
                        null,
                        Progress.IsBeingIgnored,
                        token)
                    .ConfigureAwait(false);
            }
        }

        private async Task<bool> ImageExists(CancellationToken token)
        {
            var images = await this.client.Images.ListImagesAsync(
                new ImagesListParameters
            {
                MatchName = this.Configuration.TagQualifiedImage
            }, token).ConfigureAwait(false);

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