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

        private bool useDockerImage;

        protected readonly string Authority;

        public IroncladFixture()
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (string.IsNullOrWhiteSpace(environmentName))
            {
                environmentName = "test";
            }

            var config = new ConfigurationBuilder()
#pragma warning disable CA1308 // Normalize strings to uppercase
                .AddJsonFile($"{environmentName.ToLowerInvariant()}settings.json")
#pragma warning restore CA1308 // Normalize strings to uppercase
                .Build();

            Console.WriteLine($"Environment: '{environmentName}'");

            this.Authority = config.GetValue<string>("authority");
            this.useDockerImage = config.GetValue<bool>("use_docker_image");

            this.ironcladProcess = this.useDockerImage ? this.UseIroncladProcessFromDocker() : this.StartIroncladProcessFromSource();
        }

        public void Dispose()
        {
            Console.WriteLine($"{nameof(IroncladFixture)} cleanup.");

            if (this.useDockerImage)
            {
                return;
            }

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
        private Process StartIroncladProcessFromSource()
        {
            Console.WriteLine("Starting Ironclad process from source...");
            var path = string.Format(
                CultureInfo.InvariantCulture,
                "..{0}..{0}..{0}..{0}..{0}Ironclad{0}Ironclad.csproj",
                Path.DirectorySeparatorChar);

            Process.Start(
                new ProcessStartInfo("dotnet", $"run -p {path} --connectionString '{PostgresFixture.ConnectionString}'")
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
                        using (var response = client.GetAsync(new Uri(this.Authority + "/api")).GetAwaiter().GetResult())
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

        private Process UseIroncladProcessFromDocker()
        {
            Console.WriteLine("Using Ironclad process from Docker container.");
            Console.WriteLine("Let's give some time for Ironclad to spin up...");

            using (var client = new HttpClient())
            {
                var attempt = 0;
                while (true)
                {
                    Thread.Sleep(500);
                    try
                    {
                        using (var response = client.GetAsync(new Uri(this.Authority + "/api")).GetAwaiter().GetResult())
                        {
                            var api = JsonConvert.DeserializeObject<IroncladApi>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult(), GetJsonSerializerSettings());
                            int.Parse(api.ProcessId, CultureInfo.InvariantCulture);
                        }

                        break;
                    }
                    catch (HttpRequestException)
                    {
                        if (++attempt >= 40)
                        {
                            Console.WriteLine("It seems that Ironclad is not running");
                            throw;
                        }
                    }
                }
            }

            return null;
        }

#pragma warning disable CA1812
        private class IroncladApi
        {
            public string ProcessId { get; set; }
        }
    }
}