// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Sdk
{
    using Xunit;

    [Collection("Ironclad")]
    public class IntegrationTest : AuthenticationTest
    {
        private readonly IroncladFixture ironcladFixture;

        public IntegrationTest(AuthenticationFixture authenticationFixture, IroncladFixture ironcladFixture)
            : base(authenticationFixture)
        {
            this.ironcladFixture = ironcladFixture;
        }
    }
}
