// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Sdk
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading;
    using Microsoft.Extensions.Configuration;
    using Npgsql;

    public sealed class PostgresFixture : IDisposable
    {
        internal const string ConnectionString = "Host=localhost;Database=ironclad;Username=postgres;Password=postgres;";

        private static readonly string DockerContainerId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture).Substring(12);

        private readonly Process process;

        private readonly bool useDockerImage;

        public PostgresFixture()
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

            var authority = config.GetValue<string>("authority");
            this.useDockerImage = config.GetValue<bool>("use_docker_image");

            if (!this.useDockerImage)
            {
                Console.WriteLine("Starting Postgres process...");
                this.process = this.StartPostgresProcess();
            }
            else
            {
                Console.WriteLine("Using docker image - Postgres process will not be initiated.");
            }
        }

        public void Dispose()
        {
            Console.WriteLine($"{nameof(PostgresFixture)} cleanup.");

            if (this.useDockerImage)
            {
                return;
            }

            try
            {
                this.process.Kill();
            }
            catch (InvalidOperationException)
            {
            }

            this.process.Dispose();
        }

        private Process StartPostgresProcess()
        {
            var process = Process.Start(
                new ProcessStartInfo("docker", $"run --rm --name {DockerContainerId} -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=ironclad -p 5432:5432 postgres:10.1-alpine")
                {
                    UseShellExecute = true,
                });

            // NOTE (Cameron): Trying to find a sensible value here so as to not throw during a debug session.
            Thread.Sleep(4000);

            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                var attempt = 0;
                while (true)
                {
                    Thread.Sleep(500);
                    try
                    {
                        connection.Open();
                        break;
                    }
                    catch (Exception ex) when (ex is NpgsqlException || ex is SocketException || ex is EndOfStreamException)
                    {
                        if (++attempt >= 20)
                        {
                            throw;
                        }
                    }
                }
            }

            return process;
        }
    }
}