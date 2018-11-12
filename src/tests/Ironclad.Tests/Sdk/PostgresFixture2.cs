using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Npgsql;
using Xunit;

namespace Ironclad.Tests.Sdk
{
    public class PostgresFixture2 : IAsyncLifetime
    {
        internal static readonly string ConnectionString =
            "Host=localhost;Database=ironclad;Username=postgres;Password=postgres;";
        
        private readonly LocalDockerContainer _container;

        public PostgresFixture2()
        {
            _container = WellKnownContainers.CreatePostgres(new NpgsqlConnectionStringBuilder(ConnectionString));
        }

        public Task InitializeAsync() => _container.StartAsync();

        public async Task DisposeAsync()
        {
            await _container.StopAsync();
            _container.Dispose();
        }
    }
}