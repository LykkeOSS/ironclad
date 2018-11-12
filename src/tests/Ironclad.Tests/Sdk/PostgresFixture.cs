using System;
using System.Threading.Tasks;
using Npgsql;

namespace Ironclad.Tests.Sdk
{
    public class PostgresFixture : IPostgresFixture
    {
        private readonly IPostgresFixture _fixture;
        
        public PostgresFixture()
        {
            if (Environment.GetEnvironmentVariable("CI") != null)
            {
                _fixture = new DockerComposedPostgres();
            }
            else
            {
                _fixture = new DockerizedPostgres();
            }
        }

        public NpgsqlConnectionStringBuilder ConnectionStringBuilder => _fixture.ConnectionStringBuilder;

        public Task InitializeAsync()
        {
            return _fixture.InitializeAsync();
        }

        public Task DisposeAsync()
        {
            return _fixture.DisposeAsync();
        }
    }
}