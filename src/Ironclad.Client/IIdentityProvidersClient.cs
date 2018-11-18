// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Client
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Exposes the public members of the identity providers client.
    /// </summary>
    public interface IIdentityProvidersClient
    {
        /// <summary>
        /// Gets the identity provider summaries (or a subset thereof).
        /// </summary>
        /// <param name="startsWith">The start of the provider name.</param>
        /// <param name="start">The zero-based start ordinal of the provider set to return.</param>
        /// <param name="size">The total size of the resource set.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The identity provider summaries.</returns>
        Task<ResourceSet<IdentityProviderSummary>> GetIdentityProviderSummariesAsync(string startsWith = default, int start = 0, int size = 20, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the identity provider.
        /// </summary>
        /// <param name="providerName">The provider name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The identity provider.</returns>
        Task<IdentityProvider> GetIdentityProviderAsync(string providerName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds the specified identity provider.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task object representing the asynchronous operation.</returns>
        Task AddIdentityProviderAsync(IdentityProvider provider, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes the specified identity provider.
        /// </summary>
        /// <param name="providerName">The provider name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task object representing the asynchronous operation.</returns>
        Task RemoveIdentityProviderAsync(string providerName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Modifies the specified identity provider.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task object representing the asynchronous operation.</returns>
        Task ModifyIdentityProviderAsync(IdentityProvider provider, CancellationToken cancellationToken = default);
    }
}