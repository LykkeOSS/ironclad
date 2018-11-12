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

    internal class LocalDockerContainer : IDisposable
    {
        private const string UnixPipe = "unix:///var/run/docker.sock";
        private const string WindowsPipe = "npipe://./pipe/docker_engine";

        private readonly DockerClientConfiguration _clientConfiguration =
            new DockerClientConfiguration(
                new Uri(Environment.OSVersion.Platform.Equals(PlatformID.Unix) ? UnixPipe : WindowsPipe)
            );

        private readonly DockerClient _client;
        private readonly LocalDockerContainerConfiguration _configuration;

        public LocalDockerContainer(LocalDockerContainerConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (string.IsNullOrEmpty(configuration.Image))
            {
                throw new ArgumentException("Please specify the Image the container is based on.",
                    nameof(configuration));
            }

            if (string.IsNullOrEmpty(configuration.Tag))
            {
                throw new ArgumentException("Please specify the Tag of the Image the container is based on.",
                    nameof(configuration));
            }

            if (string.IsNullOrEmpty(configuration.ContainerName))
            {
                throw new ArgumentException("Please specify the ContainerName of the container.",
                    nameof(configuration));
            }

            if (configuration.ContainerPortBindings == null)
            {
                throw new ArgumentException(
                    "Please specify either an empty or filled list of ContainerPortBindings for the container.",
                    nameof(configuration));
            }

            if (configuration.ContainerEnvironmentVariables == null)
            {
                throw new ArgumentException(
                    "Please specify either an empty or filled list of ContainerEnvironmentVariables for the container.",
                    nameof(configuration));
            }

            if (configuration.WaitUntilAvailable == null)
            {
                throw new ArgumentException("Please specify the WaitUntilAvailable action to execute on the container.",
                    nameof(configuration));
            }

            if (configuration.MaximumWaitUntilAvailableAttempts <= 0)
            {
                throw new ArgumentException(
                    "Please specify a MaximumWaitUntilAvailableAttempts greater than or equal to 1.",
                    nameof(configuration));
            }

            if (configuration.TimeBetweenWaitUntilAvailableAttempts < TimeSpan.Zero)
            {
                throw new ArgumentException(
                    "Please specify a TimeBetweenWaitUntilAvailableAttempts greater than or equal to TimeSpan.Zero.",
                    nameof(configuration));
            }

            _client = _clientConfiguration.CreateClient();
            _configuration = configuration;
        }

        public async Task StartAsync(CancellationToken token = default)
        {
            if (_configuration.IsContainerReusable)
            {
                var id = await TryFindContainer(token);
                if (id == null)
                {
                    await AutoCreateImage(token);
                    id = await CreateContainer(token);
                }

                await StartContainer(id, token);
            }
            else
            {
                await AutoCreateImage(token);
                await StartContainer(await CreateContainer(token), token);
            }
        }

        public async Task StopAsync(CancellationToken token = default)
        {
            var id = await TryFindContainer(token);
            if (id != null)
            {
                await StopContainer(id, token);
                if(_configuration.AutoRemoveContainer)
                {
                    await RemoveContainer(id, token);
                }
            }
        }

        private async Task<string> TryFindContainer(CancellationToken token)
        {
            var containers = await _client.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["name"] = new Dictionary<string, bool>
                    {
                        [_configuration.ContainerName] = true
                    }
                }
            }, token).ConfigureAwait(false);

            return containers
                .FirstOrDefault(container => container.State != "exited")
                ?.ID;
        }

        private async Task<string> CreateContainer(CancellationToken token)
        {
            var portBindings = _configuration.ContainerPortBindings.ToDictionary(
                binding => $"{binding.GuestTcpPort}/tcp",
                binding => (IList<PortBinding>) new List<PortBinding>
                {
                    new PortBinding
                    {
                        HostPort = binding.HostTcpPort.ToString(CultureInfo.InvariantCulture)
                    }
                });

            var parameters = new CreateContainerParameters
            {
                Image = _configuration.TagQualifiedImage,
                Name = _configuration.ContainerName,
                Tty = true,
                Env = _configuration.ContainerEnvironmentVariables,
                HostConfig = new HostConfig
                {
                    PortBindings = portBindings
                }
            };

            var container = await _client.Containers
                .CreateContainerAsync(parameters, token)
                .ConfigureAwait(false);

            return container.ID;
        }

        private async Task StartContainer(string id, CancellationToken token)
        {
            var started = await _client.Containers
                .StartContainerAsync(id, new ContainerStartParameters(), token)
                .ConfigureAwait(false);

            if (started)
            {
                var attempt = 0;
                while (
                    attempt < _configuration.MaximumWaitUntilAvailableAttempts &&
                    !await _configuration.WaitUntilAvailable(token).ConfigureAwait(false))
                {
                    if (attempt != _configuration.MaximumWaitUntilAvailableAttempts - 1)
                    {
                        await Task
                            .Delay(_configuration.TimeBetweenWaitUntilAvailableAttempts, token)
                            .ConfigureAwait(false);
                    }

                    attempt++;
                }

                if (attempt == _configuration.MaximumWaitUntilAvailableAttempts)
                {
                    throw new Exception(
                        $"The container {_configuration.ContainerName} did not become available in a timely fashion.");
                }
            }
        }

        private async Task StopContainer(string id, CancellationToken token)
        {
            await _client.Containers
                .StopContainerAsync(id, new ContainerStopParameters {WaitBeforeKillSeconds = 5}, token)
                .ConfigureAwait(false);
        }

        private async Task RemoveContainer(string id, CancellationToken token)
        {
            await _client.Containers
                .RemoveContainerAsync(id, new ContainerRemoveParameters {Force = false}, token)
                .ConfigureAwait(false);
        }

        private async Task AutoCreateImage(CancellationToken token)
        {
            if (!await ImageExists(token))
            {
                await _client
                    .Images
                    .CreateImageAsync(
                        new ImagesCreateParameters
                        {
                            FromImage = _configuration.Image,
                            Tag = _configuration.Tag
                        },
                        null,
                        Progress.IsBeingIgnored,
                        token
                    )
                    .ConfigureAwait(false);
            }
        }

        private async Task<bool> ImageExists(CancellationToken token)
        {
            var images = await _client.Images.ListImagesAsync(new ImagesListParameters
            {
                MatchName = _configuration.TagQualifiedImage
            }, token).ConfigureAwait(false);

            return images.Count != 0;
        }

        public void Dispose()
        {
            _client?.Dispose();
            _clientConfiguration.Dispose();
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