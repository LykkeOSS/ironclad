// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Client
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// An HTTP client for managing roles for users of the authorization server.
    /// </summary>
    public sealed class RolesHttpClient : HttpClientBase,  IRolesClient
    {
        private const string ApiPath = "/api/roles";

        /// <summary>
        /// Initializes a new instance of the <see cref="RolesHttpClient"/> class.
        /// </summary>
        /// <param name="authority">The authority.</param>
        /// <param name="innerHandler">The inner handler.</param>
        public RolesHttpClient(string authority, HttpMessageHandler innerHandler = null)
            : base(authority, innerHandler)
        {
        }

        /// <inheritdoc />
        public async Task<ResourceSet<string>> GetRolesAsync(string startsWith = default, int start = 0, int size = 20, CancellationToken cancellationToken = default)
        {
            var resourceSet = await this.GetAsync<ResourceSet<Role>>(
                this.RelativeUrl($"{ApiPath}?name={WebUtility.UrlEncode(startsWith)}&skip={NotNegative(start, nameof(start))}&take={NotNegative(size, nameof(size))}"),
                cancellationToken)
                .ConfigureAwait(false);

            return new ResourceSet<string>(resourceSet.Start, resourceSet.TotalSize, resourceSet.Select(role => role.Name));
        }

        /// <inheritdoc />
        public async Task<bool> RoleExistsAsync(string role, CancellationToken cancellationToken = default)
        {
            var url = this.RelativeUrl($"/api/roles/{WebUtility.UrlEncode(NotNull(role, nameof(role)))}");

            try
            {
                using (var response = await this.Client.HeadAsync(url, cancellationToken).ConfigureAwait(false))
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return false;
                    }

                    await response.EnsureSuccess().ConfigureAwait(false);

                    return true;
                }
            }
            catch (HttpRequestException ex)
            {
                throw new HttpException(HttpMethod.Head, new Uri(url), ex);
            }
        }

        /// <inheritdoc />
        public Task AddRoleAsync(string role, CancellationToken cancellationToken = default) =>
            this.SendAsync(HttpMethod.Post, this.RelativeUrl(ApiPath), new Role { Name = NotNull(role, nameof(role)) }, cancellationToken);

        /// <inheritdoc />
        public Task RemoveRoleAsync(string role, CancellationToken cancellationToken = default) =>
            this.DeleteAsync(this.RelativeUrl($"{ApiPath}/{WebUtility.UrlEncode(NotNull(role, nameof(role)))}"), cancellationToken);
    }
}