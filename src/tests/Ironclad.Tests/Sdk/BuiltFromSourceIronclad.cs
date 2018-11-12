// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Sdk
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public class BuiltFromSourceIronclad : IIroncladFixture
    {
        private readonly string _authority;
        private readonly string _connectionString;
        private Process _process;

        public BuiltFromSourceIronclad(string authority, string connectionString)
        {
            _authority = authority ?? throw new ArgumentNullException(nameof(authority));
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task InitializeAsync()
        {
            var path = string.Format(
                CultureInfo.InvariantCulture,
                "..{0}..{0}..{0}..{0}..{0}Ironclad{0}Ironclad.csproj",
                Path.DirectorySeparatorChar);

            _process = Process.Start(
                new ProcessStartInfo("dotnet", $"run -p {path} --connectionString '{_connectionString}'")
                {
                    UseShellExecute = true
                });

            async Task<bool> WaitUntilAvailable(CancellationToken token)
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        using (var response = await client.GetAsync(new Uri(_authority + "/api"), token)
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

            const int maximumWaitUntilAvailableAttempts = 30;
            var timeBetweenWaitUntilAvailableAttempts = TimeSpan.FromSeconds(2);
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
            try
            {
                _process.Kill();
            }
            catch(Win32Exception)
            {
            }
            _process.Dispose();
            return Task.CompletedTask;
        }
    }
}