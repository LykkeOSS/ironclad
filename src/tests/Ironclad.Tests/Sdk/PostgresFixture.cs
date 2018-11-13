// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Sdk
{
    using System;
    using System.Threading.Tasks;
    using Npgsql;

    public class PostgresFixture : IPostgresFixture
    {
        private readonly IPostgresFixture fixture;

        public PostgresFixture()
        {
            if (Environment.GetEnvironmentVariable("CI") != null)
            {
                this.fixture = new DockerComposedPostgres();
            }
            else
            {
                this.fixture = new DockerizedPostgres();
            }
        }

        public NpgsqlConnectionStringBuilder ConnectionStringBuilder => this.fixture.ConnectionStringBuilder;

        public Task InitializeAsync()
        {
            return this.fixture.InitializeAsync();
        }

        public Task DisposeAsync()
        {
            return this.fixture.DisposeAsync();
        }
    }
}