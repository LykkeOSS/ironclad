// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Feature
{
    using IdentityModel.Client;
    using Ironclad.Tests.Sdk;
    using Xbehave;

    public class ServerApplicationFeature : IntegrationTest
    {
        public ServerApplicationFeature(AuthenticationFixture securityFixture, IroncladFixture ironcladFixture)
            : base(securityFixture, ironcladFixture)
        {
        }

        [Scenario(Skip = "Incomplete")]
        public void CanCallSecureWebApi()
        {
            "Given some hard-coded credentials eg. bob/bob"
                .x(async () =>
                {
                    var discoveryClient = new DiscoveryClient("http://localhost:5005");
                    var discoveryResponse = await discoveryClient.GetAsync().ConfigureAwait(false);
                });

            "And some restricted data".x(
                () =>
                {
                });

            "When I navigate to the secure [sample] website"
                .x(() =>
                {
                });

            "And I log in using the credentials".x(() => { });

            "Then I am able to view the restricted data".x(() => { });
        }
    }
}
