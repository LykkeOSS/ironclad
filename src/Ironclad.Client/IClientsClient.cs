// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Client
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Exposes the public members of the clients client.
    /// </summary>
    public interface IClientsClient
    {
        /// <summary>
        /// Gets the client summaries (or a subset thereof).
        /// </summary>
        /// <param name="startsWith">The start of the client identifier.</param>
        /// <param name="start">The zero-based start ordinal of the client set to return.</param>
        /// <param name="size">The total size of the client set.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The client summaries.</returns>
        Task<ResourceSet<ClientSummary>> GetClientSummariesAsync(string startsWith = default, int start = default, int size = 20, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the specified client.
        /// </summary>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The client.</returns>
        Task<Client> GetClientAsync(string clientId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds the specified client.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task object representing the asynchronous operation.</returns>
        Task AddClientAsync(Client client, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes the specified client.
        /// </summary>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task object representing the asynchronous operation.</returns>
        Task RemoveClientAsync(string clientId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Modifies the specified client.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task object representing the asynchronous operation.</returns>
        Task ModifyClientAsync(Client client, CancellationToken cancellationToken = default);
    }
}