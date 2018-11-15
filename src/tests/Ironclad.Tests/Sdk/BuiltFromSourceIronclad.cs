// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Sdk
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Npgsql;

    public class BuiltFromSourceIronclad : IIroncladFixture
    {
        private readonly string authority;
        private readonly string connectionString;
        private Process process;

        public BuiltFromSourceIronclad(string authority, string connectionString)
        {
            this.authority = authority ?? throw new ArgumentNullException(nameof(authority));
            this.connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task InitializeAsync()
        {
            var path = string.Format(
                CultureInfo.InvariantCulture,
                "..{0}..{0}..{0}..{0}..{0}Ironclad{0}Ironclad.csproj",
                Path.DirectorySeparatorChar);

            var arguments = Environment.OSVersion.Platform.Equals(PlatformID.Unix)
                ? $"run -p {path} -- --connectionString '{this.connectionString}'"
                : $"run -p {path} --connectionString '{this.connectionString}'";
            this.process = Process.Start(
                new ProcessStartInfo(
                    "dotnet",
                    arguments)
                {
                    UseShellExecute = !Environment.OSVersion.Platform.Equals(PlatformID.Unix)
                });

            async Task<bool> WaitUntilAvailable(CancellationToken token)
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        using (var response = await client.GetAsync(new Uri(this.authority + "/api"), token)
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
            }

            const int maximumWaitUntilAvailableAttempts = 15;
            var timeBetweenWaitUntilAvailableAttempts = TimeSpan.FromSeconds(1);
            var attempt = 0;
            while (
                attempt < maximumWaitUntilAvailableAttempts &&
                !await WaitUntilAvailable(default).ConfigureAwait(false))
            {
                if (attempt != maximumWaitUntilAvailableAttempts - 1)
                {
                    await Task
                        .Delay(timeBetweenWaitUntilAvailableAttempts, default)
                        .ConfigureAwait(false);
                }

                attempt++;
            }

            if (attempt == maximumWaitUntilAvailableAttempts)
            {
                throw new Exception(
                    "The Ironclad instance did not become available in a timely fashion.");
            }
        }

        public Task DisposeAsync()
        {
            if (this.process != null)
            {
                try
                {
                    if (Environment.OSVersion.Platform.Equals(PlatformID.Unix))
                    {
                        using (var killer = Process.Start(new ProcessStartInfo("pkill", $"-TERM -P {this.process.Id}")))
                        {
                            killer?.WaitForExit();
                        }
                    }
                    else
                    {
                        using (var killer =
                            Process.Start(new ProcessStartInfo("taskkill", $"/T /F /PID {this.process.Id}")))
                        {
                            killer?.WaitForExit();
                        }
                    }
                }
                catch (Win32Exception)
                {
                }
            }

            this.process?.Dispose();
            return Task.CompletedTask;
        }
    }
}