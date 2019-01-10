// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Client
{
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// An HTTP client for managing identity resources for the authorization server.
    /// </summary>
    public sealed class IdentityResourcesHttpClient : HttpClientBase, IIdentityResourcesClient
    {
        private const string ApiPath = "/api/identityresources";

        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityResourcesHttpClient"/> class.
        /// </summary>
        /// <param name="authority">The authority.</param>
        /// <param name="innerHandler">The inner handler.</param>
        public IdentityResourcesHttpClient(string authority, HttpMessageHandler innerHandler = null)
            : base(authority, innerHandler)
        {
        }

        /// <inheritdoc />
        public Task<ResourceSet<ResourceSummary>> GetIdentityResourceSummariesAsync(
            string startsWith = default,
            int start = 0,
            int size = 20,
            CancellationToken cancellationToken = default) =>
            this.GetAsync<ResourceSet<ResourceSummary>>(
                this.RelativeUrl($"{ApiPath}?name={WebUtility.UrlEncode(startsWith)}&skip={NotNegative(start, nameof(start))}&take={NotNegative(size, nameof(size))}"),
                cancellationToken);

        /// <inheritdoc />
        public Task<IdentityResource> GetIdentityResourceAsync(string resourceName, CancellationToken cancellationToken = default) =>
            this.GetAsync<IdentityResource>(this.RelativeUrl($"{ApiPath}/{WebUtility.UrlEncode(NotNull(resourceName, nameof(resourceName)))}"), cancellationToken);

        /// <inheritdoc />
        public Task AddIdentityResourceAsync(IdentityResource resource, CancellationToken cancellationToken = default) =>
            this.SendAsync(HttpMethod.Post, this.RelativeUrl(ApiPath), resource, cancellationToken);

        /// <inheritdoc />
        public Task RemoveIdentityResourceAsync(string resourceName, CancellationToken cancellationToken = default) =>
            this.DeleteAsync(this.RelativeUrl($"{ApiPath}/{WebUtility.UrlEncode(NotNull(resourceName, nameof(resourceName)))}"), cancellationToken);

        /// <inheritdoc />
        public Task ModifyIdentityResourceAsync(IdentityResource resource, CancellationToken cancellationToken = default) =>
            this.SendAsync(
                HttpMethod.Put,
                this.RelativeUrl($"{ApiPath}/{WebUtility.UrlEncode(NotNull(resource?.Name, "resource.Name"))}"),
                resource,
                cancellationToken);
    }
}