// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Client
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Exposes the public members of the identity resources client.
    /// </summary>
    public interface IIdentityResourcesClient
    {
        /// <summary>
        /// Gets the identity resource summaries (or a subset thereof).
        /// </summary>
        /// <param name="startsWith">The start of the resource name.</param>
        /// <param name="start">The zero-based start ordinal of the resource set to return.</param>
        /// <param name="size">The total size of the resource set.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The resource summaries.</returns>
        Task<ResourceSet<ResourceSummary>> GetIdentityResourceSummariesAsync(string startsWith = default, int start = 0, int size = 20, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the identity resource.
        /// </summary>
        /// <param name="resourceName">The resource name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The resource.</returns>
        Task<IdentityResource> GetIdentityResourceAsync(string resourceName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds the specified identity resource.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task object representing the asynchronous operation.</returns>
        Task AddIdentityResourceAsync(IdentityResource resource, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes the specified identity resource.
        /// </summary>
        /// <param name="resourceName">The resource name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task object representing the asynchronous operation.</returns>
        Task RemoveIdentityResourceAsync(string resourceName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Modifies the specified identity resource.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task object representing the asynchronous operation.</returns>
        Task ModifyIdentityResourceAsync(IdentityResource resource, CancellationToken cancellationToken = default);
    }
}