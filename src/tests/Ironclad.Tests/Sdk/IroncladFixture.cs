// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Sdk
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Xunit.Abstractions;

    public class IroncladFixture : IIroncladFixture
    {
        private readonly PostgresFixture _postgres;
        private readonly IIroncladFixture _ironclad;

        public IroncladFixture()
        {
            _postgres = new PostgresFixture();
            
            var configFile = Path.Combine(
                Path.GetDirectoryName(typeof(IroncladFixture).Assembly.Location),
                "testsettings.json");
            var config = new ConfigurationBuilder().AddJsonFile(configFile).Build();

            var authority = config.GetValue<string>("authority");
            var useDockerImage = config.GetValue<bool>("use_docker_image");

            if (Environment.GetEnvironmentVariable("CI") == null || useDockerImage)
            {
                _ironclad = new BuiltFromSourceIronclad(authority, _postgres.ConnectionStringBuilder.ConnectionString);
            }
            else
            {
                _ironclad = new DockerizedIronclad(authority, _postgres.ConnectionStringBuilder.ConnectionString);
            }
        }
        
        public async Task InitializeAsync()
        {
            await _postgres.InitializeAsync();
            await _ironclad.InitializeAsync();
        }

        public async Task DisposeAsync()
        {
            await _postgres.DisposeAsync();
            await _ironclad.DisposeAsync();
        }
    }
}