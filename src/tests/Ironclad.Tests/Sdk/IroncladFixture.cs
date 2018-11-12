// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Sdk
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;

    public sealed class IroncladFixture : IDisposable
    {
        private static readonly string DockerContainerId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture).Substring(12);

        private readonly Process ironcladProcess;

        public IroncladFixture()
        {
            var configFile = Path.Combine(Path.GetDirectoryName(typeof(IroncladFixture).Assembly.Location), "testsettings.json");
            var config = new ConfigurationBuilder().AddJsonFile(configFile).Build();

            var authority = config.GetValue<string>("authority");
            var useDockerImage = config.GetValue<bool>("use_docker_image");

            this.ironcladProcess = useDockerImage ? this.StartIroncladProcessFromDocker(authority) : this.StartIroncladProcessFromSource(authority);
        }

        public void Dispose()
        {
            try
            {
                this.ironcladProcess.Kill();
            }
            catch (InvalidOperationException)
            {
            }

            this.ironcladProcess.Dispose();
        }

        private static JsonSerializerSettings GetJsonSerializerSettings()
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() },
                NullValueHandling = NullValueHandling.Ignore,
            };

            settings.Converters.Add(new StringEnumConverter());

            return settings;
        }

        [DebuggerStepThrough]
        private Process StartIroncladProcessFromSource(string authority)
        {
            var path = string.Format(
                CultureInfo.InvariantCulture,
                "..{0}..{0}..{0}..{0}..{0}Ironclad{0}Ironclad.csproj",
                Path.DirectorySeparatorChar);

            Process.Start(
                new ProcessStartInfo("dotnet", $"run -p {path} --connectionString '{PostgresFixture2.ConnectionString}'")
                {
                    UseShellExecute = true,
                });

            var processId = default(int);
            using (var client = new HttpClient())
            {
                var attempt = 0;
                while (true)
                {
                    Thread.Sleep(500);
                    try
                    {
                        using (var response = client.GetAsync(new Uri(authority + "/api")).GetAwaiter().GetResult())
                        {
                            var api = JsonConvert.DeserializeObject<IroncladApi>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult(), GetJsonSerializerSettings());
                            processId = int.Parse(api.ProcessId, CultureInfo.InvariantCulture);
                        }

                        break;
                    }
                    catch (HttpRequestException)
                    {
                        if (++attempt >= 20)
                        {
                            throw;
                        }
                    }
                }
            }

            return Process.GetProcessById(processId);
        }

        private Process StartIroncladProcessFromDocker(string authority)
        {
            var process = Process.Start(
                ////new ProcessStartInfo("docker", $"run --rm --name {DockerContainerId} -e IRONCLAD_CONNECTIONSTRING={PostgresFixture.ConnectionString} -p 5005:80 ironclad:dev")
                new ProcessStartInfo("docker-compose", $"run --rm --name {DockerContainerId} -e IRONCLAD_CONNECTIONSTRING={PostgresFixture2.ConnectionString} -p 5005:80 ironclad:dev")
                {
                    UseShellExecute = true,
                });

            Thread.Sleep(1000);

            var processId = default(int);
            using (var client = new HttpClient())
            {
                var attempt = 0;
                while (true)
                {
                    Thread.Sleep(500);
                    try
                    {
                        using (var response = client.GetAsync(new Uri(authority + "/api")).GetAwaiter().GetResult())
                        {
                            var api = JsonConvert.DeserializeObject<IroncladApi>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult(), GetJsonSerializerSettings());
                            processId = int.Parse(api.ProcessId, CultureInfo.InvariantCulture);
                        }

                        break;
                    }
                    catch (HttpRequestException)
                    {
                        if (++attempt >= 20)
                        {
                            throw;
                        }
                    }
                }
            }

            return Process.GetProcessById(processId);
        }

#pragma warning disable CA1812
        private class IroncladApi
        {
            public string ProcessId { get; set; }
        }
    }
}