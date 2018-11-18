// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Client
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Exposes the public members of the API resources client.
    /// </summary>
    public interface IApiResourcesClient
    {
        /// <summary>
        /// Gets the API resource summaries (or a subset thereof).
        /// </summary>
        /// <param name="startsWith">The start of the resource name.</param>
        /// <param name="start">The zero-based start ordinal of the resource set to return.</param>
        /// <param name="size">The total size of the resource set.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The resource summaries.</returns>
        Task<ResourceSet<ResourceSummary>> GetApiResourceSummariesAsync(string startsWith = default, int start = 0, int size = 20, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the specified API resource.
        /// </summary>
        /// <param name="resourceName">The resource name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The resource.</returns>
        Task<ApiResource> GetApiResourceAsync(string resourceName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds the specified API resource.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task object representing the asynchronous operation.</returns>
        Task AddApiResourceAsync(ApiResource resource, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes the specified API resource.
        /// </summary>
        /// <param name="resourceName">The resource name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task object representing the asynchronous operation.</returns>
        Task RemoveApiResourceAsync(string resourceName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Modifies the specified API resource.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task object representing the asynchronous operation.</returns>
        Task ModifyApiResourceAsync(ApiResource resource, CancellationToken cancellationToken = default);
    }
}