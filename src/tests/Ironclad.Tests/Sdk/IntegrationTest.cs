// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Sdk
{
    using Xunit;

    [Collection("Ironclad")]
    public class IntegrationTest : AuthenticationTest
    {
        private readonly IroncladFixture ironcladFixture;
        private readonly PostgresFixture2 postgresFixture;

        public IntegrationTest(AuthenticationFixture authenticationFixture, IroncladFixture ironcladFixture, PostgresFixture2 postgresFixture)
            : base(authenticationFixture)
        {
            this.ironcladFixture = ironcladFixture;
            this.postgresFixture = postgresFixture;
        }
    }
}
