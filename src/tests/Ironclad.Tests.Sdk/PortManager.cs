// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Sdk
{
    using System.Net;
    using System.Net.Sockets;

    // Original source: https://github.com/aspnet/KestrelHttpServer/blob/95722670c14855e3d5a59d482f01b9b38ed9dff1/test/Microsoft.AspNetCore.Server.Kestrel.TestCommon/PortManager.cs
    internal static class PortManager
    {
        private static readonly object PortLock = new object();

        public static int GetNextPort()
        {
            int port;
            lock (PortLock)
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                    port = ((IPEndPoint)socket.LocalEndPoint).Port;
                    socket.Close();
                }
            }

            return port;
        }
    }
}