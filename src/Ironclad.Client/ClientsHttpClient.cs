// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Client
{
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// An HTTP client for managing clients of the authorization server.
    /// </summary>
    public sealed class ClientsHttpClient : HttpClientBase, IClientsClient
    {
        private const string ApiPath = "/api/clients";

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientsHttpClient"/> class.
        /// </summary>
        /// <param name="authority">The authority.</param>
        /// <param name="innerHandler">The inner handler.</param>
        public ClientsHttpClient(string authority, HttpMessageHandler innerHandler = null)
            : base(authority, innerHandler)
        {
        }

        /// <inheritdoc />
        public Task<ResourceSet<ClientSummary>> GetClientSummariesAsync(
            string startsWith = default,
            int start = 0,
            int size = 20,
            CancellationToken cancellationToken = default) =>
            this.GetAsync<ResourceSet<ClientSummary>>(
                this.RelativeUrl($"{ApiPath}?id={WebUtility.UrlEncode(startsWith)}&skip={NotNegative(start, nameof(start))}&take={NotNegative(size, nameof(size))}"),
                cancellationToken);

        /// <inheritdoc />
        public Task<Client> GetClientAsync(string clientId, CancellationToken cancellationToken = default) =>
            this.GetAsync<Client>(this.RelativeUrl($"{ApiPath}/{WebUtility.UrlEncode(NotNull(clientId, nameof(clientId)))}"), cancellationToken);

        /// <inheritdoc />
        public Task AddClientAsync(Client client, CancellationToken cancellationToken = default) =>
            this.SendAsync(HttpMethod.Post, this.RelativeUrl(ApiPath), client, cancellationToken);

        /// <inheritdoc />
        public Task RemoveClientAsync(string clientId, CancellationToken cancellationToken = default) =>
            this.DeleteAsync(this.RelativeUrl($"{ApiPath}/{WebUtility.UrlEncode(NotNull(clientId, nameof(clientId)))}"), cancellationToken);

        /// <inheritdoc />
        public Task ModifyClientAsync(Client client, CancellationToken cancellationToken = default) =>
            this.SendAsync(HttpMethod.Put, this.RelativeUrl($"{ApiPath}/{WebUtility.UrlEncode(NotNull(client?.Id, "client.Id"))}"), client, cancellationToken);
    }
}