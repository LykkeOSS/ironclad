// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Feature
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Threading.Tasks;
    using FluentAssertions;
    using IdentityModel.Client;
    using Ironclad.Client;
    using Ironclad.Tests.Sdk;
    using Xunit;

    public class ApiResourceManagement : IntegrationTest
    {
        public ApiResourceManagement(AuthenticationFixture securityFixture, IroncladFixture ironcladFixture)
            : base(securityFixture, ironcladFixture)
        {
        }

        [Fact]
        public async Task CanAddApiResourceMinimum()
        {
            // arrange
            var httpClient = new ApiResourcesHttpClient(this.Authority, this.Handler);
            var expectedResource = new ApiResource
            {
                Name = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                ApiSecret = "secret",
            };

            // act
            await httpClient.AddApiResourceAsync(expectedResource).ConfigureAwait(false);

            // assert
            var actualResource = await httpClient.GetApiResourceAsync(expectedResource.Name).ConfigureAwait(false);
            actualResource.Should().NotBeNull();
            actualResource.Name.Should().Be(expectedResource.Name);
        }

        [Fact]
        public async Task CanAddApiResource()
        {
            // arrange
            var httpClient = new ApiResourcesHttpClient(this.Authority, this.Handler);
            var expectedResource = new ApiResource
            {
                Name = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                DisplayName = $"{nameof(ApiResourceManagement)}.{nameof(this.CanAddApiResource)} (integration test)",
                ApiSecret = "secret",
                UserClaims = { "name", "role" },
                ApiScopes = { new ApiResource.Scope { Name = "api", UserClaims = { "profile" } } },
                Enabled = false,
            };

            // act
            await httpClient.AddApiResourceAsync(expectedResource).ConfigureAwait(false);

            // assert
            var actualResource = await httpClient.GetApiResourceAsync(expectedResource.Name).ConfigureAwait(false);
            actualResource.Should().NotBeNull();
            actualResource.Should().BeEquivalentTo(expectedResource, options => options.Excluding(resource => resource.ApiSecret));
        }

        [Fact]
        public async Task CanGetApiResourceSummaries()
        {
            // arrange
            var httpClient = new ApiResourcesHttpClient(this.Authority, this.Handler);
            var expectedResource = new ApiResource
            {
                Name = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                DisplayName = $"{nameof(ApiResourceManagement)}.{nameof(this.CanGetApiResourceSummaries)} (integration test)",
                ApiSecret = "secret",
            };

            await httpClient.AddApiResourceAsync(expectedResource).ConfigureAwait(false);

            // act
            var resourceSummaries = await httpClient.GetApiResourceSummariesAsync().ConfigureAwait(false);

            // assert
            resourceSummaries.Should().NotBeNull();
            resourceSummaries.Should().Contain(summary => summary.Name == expectedResource.Name && summary.DisplayName == expectedResource.DisplayName);
        }

        [Fact]
        public async Task CanGetApiResourceSummariesWithQuery()
        {
            // arrange
            var httpClient = new ApiResourcesHttpClient(this.Authority, this.Handler);
            var resource1 = new ApiResource { Name = "query", ApiSecret = "secret" };
            var resource2 = new ApiResource { Name = "query_test_02", ApiSecret = "secret" };
            var resource3 = new ApiResource { Name = "query_test_03", ApiSecret = "secret" };

            await httpClient.AddApiResourceAsync(resource1).ConfigureAwait(false);
            await httpClient.AddApiResourceAsync(resource2).ConfigureAwait(false);
            await httpClient.AddApiResourceAsync(resource3).ConfigureAwait(false);

            // act
            var resourceSummaries = await httpClient.GetApiResourceSummariesAsync("query_").ConfigureAwait(false);

            // assert
            resourceSummaries.Should().NotBeNull();
            resourceSummaries.Should().HaveCount(2);
            resourceSummaries.Should().Contain(summary => summary.Name == resource2.Name);
            resourceSummaries.Should().Contain(summary => summary.Name == resource3.Name);
        }

        [Fact]
        public async Task CanModifyApiResource()
        {
            // arrange
            var httpClient = new ApiResourcesHttpClient(this.Authority, this.Handler);
            var originalApiResource = new ApiResource
            {
                Name = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                DisplayName = $"{nameof(ApiResourceManagement)}.{nameof(this.CanModifyApiResource)} (integration test)",
                ApiSecret = "secret",
                UserClaims = { "name", "role" },
                ApiScopes = { new ApiResource.Scope { Name = "api", UserClaims = { "profile" } } },
                Enabled = false,
            };

            var expectedResource = new ApiResource
            {
                Name = originalApiResource.Name,
                DisplayName = $"{nameof(ApiResourceManagement)}.{nameof(this.CanModifyApiResource)} (integration test) #2",
                UserClaims = { "profile" },
                ApiScopes = { new ApiResource.Scope { Name = "test_api", UserClaims = { "name", "role" } } },
                Enabled = true,
            };

            await httpClient.AddApiResourceAsync(originalApiResource).ConfigureAwait(false);

            // act
            await httpClient.ModifyApiResourceAsync(expectedResource).ConfigureAwait(false);

            // assert
            var actualResource = await httpClient.GetApiResourceAsync(expectedResource.Name).ConfigureAwait(false);
            actualResource.Should().NotBeNull();
            actualResource.Should().BeEquivalentTo(expectedResource, options => options.Excluding(resource => resource.ApiSecret));
        }

        [Fact]
        public async Task CanRemoveApiResource()
        {
            // arrange
            var httpClient = new ApiResourcesHttpClient(this.Authority, this.Handler);
            var resource = new ApiResource
            {
                Name = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                DisplayName = $"{nameof(ApiResourceManagement)}.{nameof(this.CanRemoveApiResource)} (integration test)",
                ApiSecret = "secret",
            };

            await httpClient.AddApiResourceAsync(resource).ConfigureAwait(false);

            // act
            await httpClient.RemoveApiResourceAsync(resource.Name).ConfigureAwait(false);

            // assert
            var resourceSummaries = await httpClient.GetApiResourceSummariesAsync().ConfigureAwait(false);
            resourceSummaries.Should().NotBeNull();
            resourceSummaries.Should().NotContain(summary => summary.Name == resource.Name);
        }

        // LINK (Cameron): https://github.com/IdentityServer/IdentityServer4.AccessTokenValidation/blob/dev/src/IdentityServer4.AccessTokenValidation/IdentityServerAuthenticationOptions.cs#L231
        [Fact]
        public async Task CanUseApiResource()
        {
            // arrange
            var httpClient = new ApiResourcesHttpClient(this.Authority, this.Handler);
            var resource = new ApiResource
            {
                Name = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                ApiSecret = "secret",
            };

            await httpClient.AddApiResourceAsync(resource).ConfigureAwait(false);

            // act
            var client = new IntrospectionClient(this.Authority + "/connect/introspect", resource.Name, resource.ApiSecret);
            var response = await client.SendAsync(new IntrospectionRequest { Token = "invalid" }).ConfigureAwait(false);

            // assert
            response.IsError.Should().BeFalse();
        }

        [Fact]
        public void CannotAddInvalidApiResource()
        {
            // arrange
            var httpClient = new ApiResourcesHttpClient(this.Authority, this.Handler);
            var resource = new ApiResource
            {
                Name = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
            };

            // act
            Func<Task> func = async () => await httpClient.AddApiResourceAsync(resource).ConfigureAwait(false);

            // assert
            func.Should().Throw<HttpException>();
        }

        [Fact]
        public async Task CannotAddDuplicateApiResource()
        {
            // arrange
            var httpClient = new ApiResourcesHttpClient(this.Authority, this.Handler);
            var resource = new ApiResource
            {
                Name = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                DisplayName = $"{nameof(ApiResourceManagement)}.{nameof(this.CannotAddDuplicateApiResource)} (integration test)",
                ApiSecret = "secret",
            };

            await httpClient.AddApiResourceAsync(resource).ConfigureAwait(false);

            // act
            Func<Task> func = async () => await httpClient.AddApiResourceAsync(resource).ConfigureAwait(false);

            // assert
            func.Should().Throw<HttpException>().And.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public void CannotModifyAuthorizationServerWebApi()
        {
            // arrange
            var httpClient = new ApiResourcesHttpClient(this.Authority, this.Handler);
            var resource = new ApiResource
            {
                Name = "auth_api",
                UserClaims = Array.Empty<string>(),
            };

            // act
            Func<Task> func = async () => await httpClient.ModifyApiResourceAsync(resource).ConfigureAwait(false);

            // assert
            func.Should().Throw<HttpException>().And.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public void CannotRemoveAuthorizationServerWebApi()
        {
            // arrange
            var httpClient = new ApiResourcesHttpClient(this.Authority, this.Handler);
            var resourceName = "auth_api";

            // act
            Func<Task> func = async () => await httpClient.RemoveApiResourceAsync(resourceName).ConfigureAwait(false);

            // assert
            func.Should().Throw<HttpException>().And.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
