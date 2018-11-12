// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Sdk
{
    using Npgsql;
    using Xunit;

    public interface IPostgresFixture : IAsyncLifetime
    {
        NpgsqlConnectionStringBuilder ConnectionStringBuilder { get; }
    }
}