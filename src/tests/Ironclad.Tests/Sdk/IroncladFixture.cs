// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Sdk
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;

    public class IroncladFixture : IIroncladFixture
    {
        private readonly PostgresFixture postgres;
        private readonly IIroncladFixture ironclad;

        public IroncladFixture()
        {
            this.postgres = new PostgresFixture();
            
            var configFile = Path.Combine(
                Path.GetDirectoryName(typeof(IroncladFixture).Assembly.Location),
                "testsettings.json");
            var config = new ConfigurationBuilder().AddJsonFile(configFile).Build();

            var authority = config.GetValue<string>("authority");
            var useDockerImage = config.GetValue<bool>("use_docker_image");

            if (Environment.GetEnvironmentVariable("CI") == null || useDockerImage)
            {
                this.ironclad = new BuiltFromSourceIronclad(authority, this.postgres.ConnectionStringBuilder.ConnectionString);
            }
            else
            {
                this.ironclad = new DockerizedIronclad(authority, this.postgres.ConnectionStringBuilder.ConnectionString);
            }
        }
        
        public async Task InitializeAsync()
        {
            await this.postgres.InitializeAsync();
            await this.ironclad.InitializeAsync();
        }

        public async Task DisposeAsync()
        {
            await this.postgres.DisposeAsync();
            await this.ironclad.DisposeAsync();
        }
    }
}