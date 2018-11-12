// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Feature
{
    using Ironclad.Tests.Sdk;
    using Xbehave;
    using Xunit;

    // As a user of the system
    // In order to access system data that is specific to me
    // I want to be able to sign-in to the system
    [TestCaseOrderer("Ironclad.Tests.Sdk.OrderStrategy", "Ironclad.Tests")]
    public class InteractiveEndUserFeature : IntegrationTest
    {
        public InteractiveEndUserFeature(AuthenticationFixture securityFixture, IroncladFixture ironcladFixture, PostgresFixture2 postgresFixture)
            : base(securityFixture, ironcladFixture, postgresFixture)
        {
        }

        [Scenario(Skip = "Incomplete")]
        public void CanSignIn()
        {
            "Given some hard-coded credentials eg. bob/bob"
                .x(() => { });

            "And some restricted data".x(() => { });

            "When I navigate to the secure [sample] website"
                .x(() =>
                {
                    // using BrowseR
                });

            "And I log in using the credentials".x(() => { });

            "Then I am able to view the restricted data".x(() => { });
        }

        [Scenario(Skip = "Incomplete")]
        public void CanSignOut()
        {
            "Given some hard-coded credentials eg. bob/bob"
                .x(() => { });

            "And some restricted data".x(() => { });

            "When I navigate to the secure [sample] website"
                .x(() =>
                {
                    // using BrowseR
                });

            "And I log in using the credentials".x(() => { });

            "Then I am able to view the restricted data".x(() => { });
        }
    }
}