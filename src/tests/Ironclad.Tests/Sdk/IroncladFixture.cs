// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Sdk
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Npgsql;
    using Xunit;

    public class IroncladFixture : IIroncladFixture
    {
        private const string ConnectionString = "Host=localhost;Database=ironclad;Username=postgres;Password=postgres;";
        private readonly IAsyncLifetime postgres;
        private readonly IAsyncLifetime ironclad;

        public IroncladFixture()
        {
            var configFile = Path.Combine(
                Path.GetDirectoryName(typeof(IroncladFixture).Assembly.Location),
                "testsettings.json");
            var config = new ConfigurationBuilder().AddJsonFile(configFile).Build();

            var authority = config.GetValue<string>("authority");
            var useDockerImage = config.GetValue<bool>("use_docker_image");

            if (Environment.GetEnvironmentVariable("CI") != null)
            {
                if (Environment.GetEnvironmentVariable("POSTGRES_CONNECTIONSTRING") == null)
                {
                    throw new NotSupportedException("The POSTGRES_CONNECTIONSTRING environment variable needs to be provided when running in a CI environment");
                }
                this.postgres = null;
                this.ironclad = new BuiltFromSourceIronclad(authority, Environment.GetEnvironmentVariable("POSTGRES_CONNECTIONSTRING"));
            }
            else
            {
                this.postgres = new DockerizedPostgres(new NpgsqlConnectionStringBuilder(ConnectionString));
                this.ironclad = useDockerImage
                    ? (IAsyncLifetime)new DockerizedIronclad(authority, ConnectionString)
                    : new BuiltFromSourceIronclad(authority, ConnectionString);
            }
        }

        public async Task InitializeAsync()
        {
            if (this.postgres != null)
            {
                await this.postgres.InitializeAsync().ConfigureAwait(false);    
            }
            await this.ironclad.InitializeAsync().ConfigureAwait(false);
        }

        public async Task DisposeAsync()
        {
            await this.ironclad.DisposeAsync().ConfigureAwait(false);
            if (this.postgres != null)
            {
                await this.postgres.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}