// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Client
{
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// An HTTP client for managing identity providers for the authorization server.
    /// </summary>
    public sealed class IdentityProvidersHttpClient : HttpClientBase, IIdentityProvidersClient
    {
        private const string ApiPath = "/api/providers";

        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityProvidersHttpClient"/> class.
        /// </summary>
        /// <param name="authority">The authority.</param>
        /// <param name="innerHandler">The inner handler.</param>
        public IdentityProvidersHttpClient(string authority, HttpMessageHandler innerHandler = null)
            : base(authority, innerHandler)
        {
        }

        /// <inheritdoc />
        public Task<ResourceSet<IdentityProviderSummary>> GetIdentityProviderSummariesAsync(
            string startsWith = default,
            int start = 0,
            int size = 20,
            CancellationToken cancellationToken = default) =>
            this.GetAsync<ResourceSet<IdentityProviderSummary>>(
                this.RelativeUrl($"{ApiPath}?name={WebUtility.UrlEncode(startsWith)}&skip={NotNegative(start, nameof(start))}&take={NotNegative(size, nameof(size))}"),
                cancellationToken);

        /// <inheritdoc />
        public Task<IdentityProvider> GetIdentityProviderAsync(string providerName, CancellationToken cancellationToken = default) =>
            this.GetAsync<IdentityProvider>(this.RelativeUrl($"{ApiPath}/{WebUtility.UrlEncode(NotNull(providerName, nameof(providerName)))}"), cancellationToken);

        /// <inheritdoc />
        public Task AddIdentityProviderAsync(IdentityProvider provider, CancellationToken cancellationToken = default) =>
            this.SendAsync(HttpMethod.Post, this.RelativeUrl(ApiPath), provider, cancellationToken);

        /// <inheritdoc />
        public Task RemoveIdentityProviderAsync(string providerName, CancellationToken cancellationToken = default) =>
            this.DeleteAsync(this.RelativeUrl($"{ApiPath}/{WebUtility.UrlEncode(NotNull(providerName, nameof(providerName)))}"), cancellationToken);

        /// <inheritdoc />
        public Task ModifyIdentityProviderAsync(IdentityProvider provider, CancellationToken cancellationToken = default) =>
            this.SendAsync(
                HttpMethod.Put,
                this.RelativeUrl($"{ApiPath}/{WebUtility.UrlEncode(NotNull(provider?.Name, "provider.Name"))}"),
                provider,
                cancellationToken);
    }
}