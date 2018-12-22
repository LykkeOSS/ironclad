// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Client
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Exposes the public members of the users client.
    /// </summary>
    public interface IUsersClient
    {
        /// <summary>
        /// Get the user summaries (or a subset thereof).
        /// </summary>
        /// <param name="startsWith">The start of the username.</param>
        /// <param name="start">The zero-based start ordinal of the user set to return.</param>
        /// <param name="size">The total size of the user set.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The user summaries.</returns>
        Task<ResourceSet<UserSummary>> GetUserSummariesAsync(string startsWith = default, int start = 0, int size = 20, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the specified user.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The user.</returns>
        Task<User> GetUserAsync(string username, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds the specified user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The new user.</returns>
        Task<User> AddUserAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes the specified user.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task object representing the asynchronous operation.</returns>
        Task RemoveUserAsync(string username, CancellationToken cancellationToken = default);

        /// <summary>
        /// Modifies the specified user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="currentUsername">The current username (if different from the specified user username).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The modified user.</returns>
        Task<User> ModifyUserAsync(User user, string currentUsername = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get user claims
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list of claims.</returns>
        Task<IDictionary<string, object>> GetClaimsAsync(string username, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds new claims to the user claims list.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="claims">The list of claims to be added</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task object representing the asynchronous operation.</returns>
        Task AddClaimsAsync(string username, IDictionary<string, IEnumerable<object>> claims, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove claims from the user claims list.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="claims">The list of claims to be removed.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task object representing the asynchronous operation.</returns>
        Task RemoveClaimsAsync(string username, IDictionary<string, IEnumerable<object>> claims, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get user roles list.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list of roles.</returns>
        Task<IEnumerable<string>> GetRolesAsync(string username, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add user to roles.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="roles">The roles list to add user to.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task object representing the asynchronous operation.</returns>
        Task AddToRolesAsync(string username, IEnumerable<string> roles, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove roles from user.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="roles">The roles list to remove user from.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task object representing the asynchronous operation.</returns>
        Task RemoveFromRolesAsync(string username, IEnumerable<string> roles, CancellationToken cancellationToken = default);
    }
}
