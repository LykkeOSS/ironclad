// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Feature
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Ironclad.Client;
    using Ironclad.Tests.Sdk;
    using Xunit;

    public class IdentityProviderManagement : AuthenticationTest
    {
        public IdentityProviderManagement(AuthenticationFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public async Task CanAddProviderMinimum()
        {
            // arrange
            var httpClient = new IdentityProvidersHttpClient(this.Authority, this.Handler);
            var expectedProvider = CreateMinimumProvider();

            // act
            await httpClient.AddIdentityProviderAsync(expectedProvider).ConfigureAwait(false);

            // assert
            var actualProvider = await httpClient.GetIdentityProviderAsync(expectedProvider.Name).ConfigureAwait(false);
            actualProvider.Should().NotBeNull();
            actualProvider.Name.Should().Be(expectedProvider.Name);
            actualProvider.Authority.Should().Be(expectedProvider.Authority);
            actualProvider.ClientId.Should().Be(expectedProvider.ClientId);
        }

        [Fact]
        public async Task CanAddProvider()
        {
            var httpClient = new IdentityProvidersHttpClient(this.Authority, this.Handler);
            var expectedProvider = new IdentityProvider
            {
                Name = $"IntegrationTest{Guid.NewGuid():N}",
                DisplayName = $"{nameof(IdentityProviderManagement)}.{nameof(this.CanAddProvider)} (integration test)",
                Authority = "https://auth-test.lykkecloud.com",
                ClientId = "test-oidc",
                CallbackPath = "/test",
                AcrValues = "idp:lykke tenant:ironclad"
            };

            // act
            await httpClient.AddIdentityProviderAsync(expectedProvider).ConfigureAwait(false);

            // assert
            var actualProvider = await httpClient.GetIdentityProviderAsync(expectedProvider.Name).ConfigureAwait(false);
            actualProvider.Should().NotBeNull();
            actualProvider.Should().BeEquivalentTo(expectedProvider);
        }

        [Fact]
        public async Task CanGetProviderSummaries()
        {
            // arrange
            var httpClient = new IdentityProvidersHttpClient(this.Authority, this.Handler);
            var expectedProvider = CreateMinimumProvider();

            await httpClient.AddIdentityProviderAsync(expectedProvider).ConfigureAwait(false);

            // act
            var providerSummaries = await httpClient.GetIdentityProviderSummariesAsync().ConfigureAwait(false);

            // assert
            providerSummaries.Should().NotBeNull();
            providerSummaries.Should()
                .Contain(summary =>
                    summary.Name == expectedProvider.Name && summary.ClientId == expectedProvider.ClientId &&
                    summary.Authority == expectedProvider.Authority);
        }

        [Fact]
        public async Task CanGetClientSummariesWithQuery()
        {
            // arrange
            var httpClient = new IdentityProvidersHttpClient(this.Authority, this.Handler);
            var prefix = $"{DateTime.Now.Ticks}-query_";
            var provider1 = CreateMinimumProvider();
            var provider2 = CreateMinimumProvider(prefix);
            var provider3 = CreateMinimumProvider(prefix);

            await httpClient.AddIdentityProviderAsync(provider1).ConfigureAwait(false);
            await httpClient.AddIdentityProviderAsync(provider2).ConfigureAwait(false);
            await httpClient.AddIdentityProviderAsync(provider3).ConfigureAwait(false);

            // act
            var providerSummaries = await httpClient.GetIdentityProviderSummariesAsync(prefix).ConfigureAwait(false);

            // assert
            providerSummaries.Should().NotBeNull();
            providerSummaries.Should().HaveCount(2);
            providerSummaries.Should().Contain(summary => summary.Name == provider2.Name);
            providerSummaries.Should().Contain(summary => summary.Name == provider3.Name);
        }

        [Fact]
        public async Task CanRemoveProvider()
        {
            // arrange
            var httpClient = new IdentityProvidersHttpClient(this.Authority, this.Handler);
            var provider = CreateMinimumProvider();

            await httpClient.AddIdentityProviderAsync(provider).ConfigureAwait(false);

            // act
            await httpClient.RemoveIdentityProviderAsync(provider.Name).ConfigureAwait(false);

            // assert
            var clientSummaries = await httpClient.GetIdentityProviderSummariesAsync().ConfigureAwait(false);
            clientSummaries.Should().NotBeNull();
            clientSummaries.Should().NotContain(summary => summary.Name == provider.Name);
        }

        [Fact]
        public void CannotAddBlankProvider()
        {
            // arrange
            var httpClient = new IdentityProvidersHttpClient(this.Authority, this.Handler);
            var provider = new IdentityProvider();

            // act
            Func<Task> func = async () => await httpClient.AddIdentityProviderAsync(provider).ConfigureAwait(false);

            // assert
            func.Should().Throw<HttpException>();
        }

        [Fact]
        public void CannotAddProviderWithBadCallback()
        {
            // arrange
            var httpClient = new IdentityProvidersHttpClient(this.Authority, this.Handler);
            var provider = CreateMinimumProvider();
            provider.CallbackPath = "nonsense";

            // act
            Func<Task> func = async () => await httpClient.AddIdentityProviderAsync(provider).ConfigureAwait(false);

            // assert
            func.Should().Throw<HttpException>();
        }

        [Fact]
        public void CannotAddProviderWithNoAuthority()
        {
            // arrange
            var httpClient = new IdentityProvidersHttpClient(this.Authority, this.Handler);
            var provider = CreateMinimumProvider();
            provider.Authority = null;

            // act
            Func<Task> func = async () => await httpClient.AddIdentityProviderAsync(provider).ConfigureAwait(false);

            // assert
            func.Should().Throw<HttpException>();
        }

        [Fact]
        public void CannotAddProviderWithNoClientId()
        {
            // arrange
            var httpClient = new IdentityProvidersHttpClient(this.Authority, this.Handler);
            var provider = CreateMinimumProvider();
            provider.ClientId = null;

            // act
            Func<Task> func = async () => await httpClient.AddIdentityProviderAsync(provider).ConfigureAwait(false);

            // assert
            func.Should().Throw<HttpException>();
        }

        [Fact]
        public async Task CannotAddDuplicateProvider()
        {
            // arrange
            var httpClient = new IdentityProvidersHttpClient(this.Authority, this.Handler);
            var provider = CreateMinimumProvider();

            await httpClient.AddIdentityProviderAsync(provider).ConfigureAwait(false);

            // act
            Func<Task> func = async () => await httpClient.AddIdentityProviderAsync(provider).ConfigureAwait(false);

            // assert
            func.Should().Throw<HttpException>().And.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        private static IdentityProvider CreateMinimumProvider(string namePrefix = "")
        {
            // Would much rather use something like Autofixture and not worry about this, but for now...
            return new IdentityProvider
            {
                Name = $"{namePrefix}IntegrationTest{Guid.NewGuid():N}",
                Authority = "https://auth-test.lykkecloud.com",
                ClientId = "test-oidc"
            };
        }
    }
}