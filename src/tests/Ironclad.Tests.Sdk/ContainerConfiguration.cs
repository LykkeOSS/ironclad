// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

#pragma warning disable CA1819

namespace Ironclad.Tests.Sdk
{
    using System;
    using System.Net;

    internal class ContainerConfiguration
    {
        public string Registry { get; set; }

        public NetworkCredential RegistryCredentials { get; set; }

        public string Image { get; set; }

        public string Tag { get; set; }

        public string TagQualifiedImage => this.Image + ":" + this.Tag;

        public string RegistryQualifiedImage => this.Registry != null ? this.Registry + "/" + this.Image : this.Image;

        public string FullyQualifiedImage => this.Registry != null ? this.Registry + "/" + this.TagQualifiedImage : this.TagQualifiedImage;

        public string ContainerName { get; set; }

        public PortBinding[] ContainerPortBindings { get; set; } = Array.Empty<PortBinding>();

        public string[] ContainerEnvironmentVariables { get; set; } = Array.Empty<string>();

        public bool OutputDockerLogs { get; set; }

        public class PortBinding
        {
            public int GuestTcpPort { get; set; }

            public int HostTcpPort { get; set; }
        }
    }
}