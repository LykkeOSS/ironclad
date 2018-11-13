// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Sdk
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class LocalDockerContainerConfiguration
    {
        // Image related
        public string Image { get; set; }

        public string Tag { get; set; }

        public string TagQualifiedImage => this.Image + ":" + this.Tag;

        // Container related
        public bool IsContainerReusable { get; set; }

        public string ContainerName { get; set; }

        public bool AutoRemoveContainer { get; set; }

        // ReSharper disable once CA1819
        public LocalDockerContainerPortBinding[] ContainerPortBindings { get; set; } =
            Array.Empty<LocalDockerContainerPortBinding>();

        // ReSharper disable once CA1819
        public string[] ContainerEnvironmentVariables { get; set; } =
            Array.Empty<string>();

        // Availability related
        public Func<CancellationToken, Task<bool>> WaitUntilAvailable { get; set; }

        public int MaximumWaitUntilAvailableAttempts { get; set; }

        public TimeSpan TimeBetweenWaitUntilAvailableAttempts { get; set; }
    }
}