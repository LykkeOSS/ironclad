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

        /// <summary>
        /// Gets the identity provider summaries (or a subset thereof).
        /// </summary>
        /// <param name="startsWith">The start of the provider name.</param>
        /// <param name="start">The zero-based start ordinal of the provider set to return.</param>
        /// <param name="size">The total size of the resource set.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The identity provider summaries.</returns>
        public Task<ResourceSet<IdentityProviderSummary>> GetIdentityProviderSummariesAsync(
            string startsWith = default,
            int start = 0,
            int size = 20,
            CancellationToken cancellationToken = default) =>
            this.GetAsync<ResourceSet<IdentityProviderSummary>>(
                this.RelativeUrl($"{ApiPath}?name={WebUtility.UrlEncode(startsWith)}&skip={NotNegative(start, nameof(start))}&take={NotNegative(size, nameof(size))}"),
                cancellationToken);

        /// <summary>
        /// Gets the identity provider.
        /// </summary>
        /// <param name="providerName">The provider name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The identity provider.</returns>
        public Task<IdentityProvider> GetIdentityProviderAsync(string providerName, CancellationToken cancellationToken = default) =>
            this.GetAsync<IdentityProvider>(this.RelativeUrl($"{ApiPath}/{WebUtility.UrlEncode(NotNull(providerName, nameof(providerName)))}"), cancellationToken);

        /// <summary>
        /// Adds the specified identity provider.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task object representing the asynchronous operation.</returns>
        public Task AddIdentityProviderAsync(IdentityProvider provider, CancellationToken cancellationToken = default) =>
            this.SendAsync<IdentityProvider>(HttpMethod.Post, this.RelativeUrl(ApiPath), provider, cancellationToken);

        /// <summary>
        /// Removes the specified identity provider.
        /// </summary>
        /// <param name="providerName">The provider name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task object representing the asynchronous operation.</returns>
        public Task RemoveIdentityProviderAsync(string providerName, CancellationToken cancellationToken = default) =>
            this.DeleteAsync(this.RelativeUrl($"{ApiPath}/{WebUtility.UrlEncode(NotNull(providerName, nameof(providerName)))}"), cancellationToken);

        /// <summary>
        /// Modifies the specified identity provider.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task object representing the asynchronous operation.</returns>
        public Task ModifyIdentityProviderAsync(IdentityProvider provider, CancellationToken cancellationToken = default) =>
            this.SendAsync<IdentityProvider>(
                HttpMethod.Put,
                this.RelativeUrl($"{ApiPath}/{WebUtility.UrlEncode(NotNull(provider?.Name, "provider.Name"))}"),
                provider,
                cancellationToken);
    }
}