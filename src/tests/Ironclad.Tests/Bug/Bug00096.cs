// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Bug
{
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Ironclad.Client;
    using Ironclad.Tests.Sdk;
    using Xunit;

    public class Bug00096 : IntegrationTest
    {
        public Bug00096(AuthenticationFixture securityFixture, IroncladFixture ironcladFixture, PostgresFixture2 postgresFixture)
            : base(securityFixture, ironcladFixture, postgresFixture)
        {
        }

        [Fact]
        public async Task ShouldCreateApiResourceWithDefaultScopeMatchingResourceName()
        {
            // arrange
            var httpClient = new ApiResourcesHttpClient(this.Authority, this.Handler);
            var expectedResource = new ApiResource
            {
                Name = "scope_name",
                ApiSecret = "secret",
                ApiScopes = null, // should default to "scope_name"
            };

            // act
            await httpClient.AddApiResourceAsync(expectedResource).ConfigureAwait(false);

            // assert
            var actualResource = await httpClient.GetApiResourceAsync(expectedResource.Name).ConfigureAwait(false);
            actualResource.Should().NotBeNull();
            actualResource.ApiScopes.Should().HaveCount(1);
            actualResource.ApiScopes.First().Name.Should().Be(expectedResource.Name);
        }
    }
}
