// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Feature
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Ironclad.Client;
    using Ironclad.Tests.Sdk;
    using Xunit;

    public class IdentityResourceManagement : IntegrationTest
    {
        public IdentityResourceManagement(AuthenticationFixture securityFixture, IroncladFixture ironcladFixture, PostgresFixture2 postgresFixture)
            : base(securityFixture, ironcladFixture, postgresFixture)
        {
        }

        [Fact]
        public async Task CanAddIdentityResourceMinimum()
        {
            // arrange
            var httpClient = new IdentityResourcesHttpClient(this.Authority, this.Handler);
            var expectedResource = new IdentityResource
            {
                Name = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                UserClaims = { "role" },
            };

            // act
            await httpClient.AddIdentityResourceAsync(expectedResource).ConfigureAwait(false);

            // assert
            var actualResource = await httpClient.GetIdentityResourceAsync(expectedResource.Name).ConfigureAwait(false);
            actualResource.Should().NotBeNull();
            actualResource.Name.Should().Be(expectedResource.Name);
            actualResource.UserClaims.Should().Contain(expectedResource.UserClaims);
        }

        [Fact]
        public async Task CanAddIdentityResource()
        {
            // arrange
            var httpClient = new IdentityResourcesHttpClient(this.Authority, this.Handler);
            var expectedResource = new IdentityResource
            {
                Name = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                DisplayName = $"{nameof(IdentityResourceManagement)}.{nameof(this.CanAddIdentityResource)} (integration test)",
                UserClaims = { "name", "role" },
                Enabled = false,
            };

            // act
            await httpClient.AddIdentityResourceAsync(expectedResource).ConfigureAwait(false);

            // assert
            var actualResource = await httpClient.GetIdentityResourceAsync(expectedResource.Name).ConfigureAwait(false);
            actualResource.Should().NotBeNull();
            actualResource.Should().BeEquivalentTo(expectedResource);
        }

        [Fact]
        public async Task CanGetIdentityResourceSummaries()
        {
            // arrange
            var httpClient = new IdentityResourcesHttpClient(this.Authority, this.Handler);
            var expectedResource = new IdentityResource
            {
                Name = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                DisplayName = $"{nameof(IdentityResourceManagement)}.{nameof(this.CanGetIdentityResourceSummaries)} (integration test)",
                UserClaims = { "role" },
            };

            await httpClient.AddIdentityResourceAsync(expectedResource).ConfigureAwait(false);

            // act
            var resourceSummaries = await httpClient.GetIdentityResourceSummariesAsync().ConfigureAwait(false);

            // assert
            resourceSummaries.Should().NotBeNull();
            resourceSummaries.Should().Contain(summary => summary.Name == expectedResource.Name && summary.DisplayName == expectedResource.DisplayName);
        }

        [Fact]
        public async Task CanGetIdentityResourceSummariesWithQuery()
        {
            // arrange
            var httpClient = new IdentityResourcesHttpClient(this.Authority, this.Handler);
            var resource1 = new IdentityResource { Name = "query", UserClaims = { "name" } };
            var resource2 = new IdentityResource { Name = "query_test_02", UserClaims = { "name" } };
            var resource3 = new IdentityResource { Name = "query_test_03", UserClaims = { "name" } };

            await httpClient.AddIdentityResourceAsync(resource1).ConfigureAwait(false);
            await httpClient.AddIdentityResourceAsync(resource2).ConfigureAwait(false);
            await httpClient.AddIdentityResourceAsync(resource3).ConfigureAwait(false);

            // act
            var resourceSummaries = await httpClient.GetIdentityResourceSummariesAsync("query_").ConfigureAwait(false);

            // assert
            resourceSummaries.Should().NotBeNull();
            resourceSummaries.Should().HaveCount(2);
            resourceSummaries.Should().Contain(summary => summary.Name == resource2.Name);
            resourceSummaries.Should().Contain(summary => summary.Name == resource3.Name);
        }

        [Fact]
        public async Task CanModifyIdentityResource()
        {
            // arrange
            var httpClient = new IdentityResourcesHttpClient(this.Authority, this.Handler);
            var originalIdentityResource = new IdentityResource
            {
                Name = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                DisplayName = $"{nameof(IdentityResourceManagement)}.{nameof(this.CanModifyIdentityResource)} (integration test)",
                UserClaims = { "name", "role" },
                Enabled = false,
            };

            var expectedResource = new IdentityResource
            {
                Name = originalIdentityResource.Name,
                DisplayName = $"{nameof(IdentityResourceManagement)}.{nameof(this.CanModifyIdentityResource)} (integration test) #2",
                UserClaims = { "profile" },
                Enabled = false,
            };

            await httpClient.AddIdentityResourceAsync(originalIdentityResource).ConfigureAwait(false);

            // act
            await httpClient.ModifyIdentityResourceAsync(expectedResource).ConfigureAwait(false);

            // assert
            var actualResource = await httpClient.GetIdentityResourceAsync(expectedResource.Name).ConfigureAwait(false);
            actualResource.Should().NotBeNull();
            actualResource.Should().BeEquivalentTo(expectedResource);
        }

        [Fact]
        public async Task CanRemoveIdentityResource()
        {
            // arrange
            var httpClient = new IdentityResourcesHttpClient(this.Authority, this.Handler);
            var resource = new IdentityResource
            {
                Name = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                DisplayName = $"{nameof(IdentityResourceManagement)}.{nameof(this.CanRemoveIdentityResource)} (integration test)",
                UserClaims = { "role" },
            };

            await httpClient.AddIdentityResourceAsync(resource).ConfigureAwait(false);

            // act
            await httpClient.RemoveIdentityResourceAsync(resource.Name).ConfigureAwait(false);

            // assert
            var resourceSummaries = await httpClient.GetIdentityResourceSummariesAsync().ConfigureAwait(false);
            resourceSummaries.Should().NotBeNull();
            resourceSummaries.Should().NotContain(summary => summary.Name == resource.Name);
        }

        [Fact]
        public void CannotAddInvalidIdentityResource()
        {
            // arrange
            var httpClient = new IdentityResourcesHttpClient(this.Authority, this.Handler);
            var resource = new IdentityResource
            {
                Name = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                DisplayName = $"{nameof(IdentityResourceManagement)}.{nameof(this.CannotAddInvalidIdentityResource)} (integration test)",
            };

            // act
            Func<Task> func = async () => await httpClient.AddIdentityResourceAsync(resource).ConfigureAwait(false);

            // assert
            func.Should().Throw<HttpException>();
        }

        [Fact]
        public async Task CannotAddDuplicateIdentityResource()
        {
            // arrange
            var httpClient = new IdentityResourcesHttpClient(this.Authority, this.Handler);
            var resource = new IdentityResource
            {
                Name = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                DisplayName = $"{nameof(IdentityResourceManagement)}.{nameof(this.CannotAddDuplicateIdentityResource)} (integration test)",
                UserClaims = { "role" },
            };

            await httpClient.AddIdentityResourceAsync(resource).ConfigureAwait(false);

            // act
            Func<Task> func = async () => await httpClient.AddIdentityResourceAsync(resource).ConfigureAwait(false);

            // assert
            func.Should().Throw<HttpException>().And.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }
    }
}
