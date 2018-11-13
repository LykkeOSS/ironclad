// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Sdk
{
    public class LocalDockerContainerPortBinding
    {
        public int GuestTcpPort { get; set; }

        public int HostTcpPort { get; set; }
    }
}