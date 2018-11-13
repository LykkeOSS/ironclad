// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Sdk
{
    using System;
    using System.Threading.Tasks;
    using Npgsql;

    public class DockerComposedPostgres : IPostgresFixture
    {
        public DockerComposedPostgres()
        {
            this.ConnectionStringBuilder = new NpgsqlConnectionStringBuilder(
                Environment.GetEnvironmentVariable("POSTGRES_CONNECTIONSTRING"));
        }
        
        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        public NpgsqlConnectionStringBuilder ConnectionStringBuilder { get; }
    }
}