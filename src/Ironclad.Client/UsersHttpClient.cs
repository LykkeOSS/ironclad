// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Client
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    /// <summary>
    /// An HTTP client for managing users of the authorization server.
    /// </summary>
    public sealed class UsersHttpClient : HttpClientBase, IUsersClient
    {
        private const string ApiPath = "/api/users";
        private const string ApiClaimsPathFmt = "/api/users/{0}/claims";
        private const string ApiRolesPathFmt = "/api/users/{0}/roles";

        /// <summary>
        /// Initializes a new instance of the <see cref="UsersHttpClient"/> class.
        /// </summary>
        /// <param name="authority">The authority.</param>
        /// <param name="innerHandler">The inner handler.</param>
        public UsersHttpClient(string authority, HttpMessageHandler innerHandler = null)
            : base(authority, innerHandler)
        {
        }

        /// <summary>
        /// Get the user summaries (or a subset thereof).
        /// </summary>
        /// <param name="startsWith">The start of the username.</param>
        /// <param name="start">The zero-based start ordinal of the user set to return.</param>
        /// <param name="size">The total size of the user set.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The user summaries.</returns>
        public Task<ResourceSet<UserSummary>> GetUserSummariesAsync(
            string startsWith = default,
            int start = 0,
            int size = 20,
            CancellationToken cancellationToken = default) =>
            this.GetAsync<ResourceSet<UserSummary>>(
                this.RelativeUrl($"{ApiPath}?username={WebUtility.UrlEncode(startsWith)}&skip={NotNegative(start, nameof(start))}&take={NotNegative(size, nameof(size))}"),
                cancellationToken);

        /// <summary>
        /// Gets the user.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The user.</returns>
        public Task<User> GetUserAsync(string username, CancellationToken cancellationToken = default) =>
            this.GetAsync<User>(this.RelativeUrl($"{ApiPath}/{WebUtility.UrlEncode(NotNull(username, nameof(username)))}"), cancellationToken);

        /// <summary>
        /// Adds the specified user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The new user.</returns>
        public async Task<User> AddUserAsync(User user, CancellationToken cancellationToken = default)
        {
            var registrationLink = default(string);

            try
            {
                using (var content = new StringContent(JsonConvert.SerializeObject(user, JsonSerializerSettings), Encoding.UTF8, "application/json"))
                using (var request = new HttpRequestMessage(HttpMethod.Post, this.RelativeUrl(ApiPath)) { Content = content })
                using (var response = await this.Client.SendAsync(request, cancellationToken).EnsureSuccess().ConfigureAwait(false))
                {
                    if (response.Content != null)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        registrationLink = JsonConvert.DeserializeObject<UserResponse>(responseContent, JsonSerializerSettings)?.RegistrationLink;

                        response.Content.Dispose();
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                throw new HttpException(HttpMethod.Post, new Uri(this.RelativeUrl(ApiPath)), ex);
            }

            user = await this.GetUserAsync(user.Username, cancellationToken).ConfigureAwait(false);
            user.RegistrationLink = registrationLink;

            return user;
        }

        /// <summary>
        /// Removes the specified user.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task object representing the asynchronous operation.</returns>
        public Task RemoveUserAsync(string username, CancellationToken cancellationToken = default) =>
            this.DeleteAsync(this.RelativeUrl($"{ApiPath}/{WebUtility.UrlEncode(NotNull(username, nameof(username)))}"), cancellationToken);

        /// <summary>
        /// Modifies the specified user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="currentUsername">The current username (if different from the specified user username).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The modified user.</returns>
        public async Task<User> ModifyUserAsync(User user, string currentUsername = null, CancellationToken cancellationToken = default)
        {
            var url = this.RelativeUrl($"{ApiPath}/{WebUtility.UrlEncode(currentUsername ?? NotNull(user?.Username, "user.Username"))}");
            await this.SendAsync<User>(HttpMethod.Put, url, user, cancellationToken).ConfigureAwait(false);
            return await this.GetUserAsync(user.Username, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<IDictionary<string, object>> GetClaimsAsync(string username, CancellationToken cancellationToken = default)
        {
            var url = this.RelativeUrl(string.Format(
                CultureInfo.InvariantCulture,
                ApiClaimsPathFmt,
                WebUtility.UrlEncode(NotNull(username, nameof(username)))));

            return this.GetAsync<IDictionary<string, object>>(url, cancellationToken);
        }

        /// <inheritdoc />
        public Task AddClaimsAsync(string username, IDictionary<string, IEnumerable<object>> claims, CancellationToken cancellationToken = default)
        {
            var url = this.RelativeUrl(string.Format(
                CultureInfo.InvariantCulture,
                ApiClaimsPathFmt,
                WebUtility.UrlEncode(NotNull(username, nameof(username)))));

            return this.SendAsync(HttpMethod.Post, url, claims, cancellationToken);
        }

        /// <inheritdoc />
        public Task RemoveClaimsAsync(string username, IDictionary<string, IEnumerable<object>> claims, CancellationToken cancellationToken = default)
        {
            var url = this.RelativeUrl(string.Format(
                CultureInfo.InvariantCulture,
                ApiClaimsPathFmt,
                WebUtility.UrlEncode(NotNull(username, nameof(username)))));

            return this.SendAsync(HttpMethod.Delete, url, claims, cancellationToken);
        }

        /// <inheritdoc />
        public Task<IEnumerable<string>> GetRolesAsync(string username, CancellationToken cancellationToken = default)
        {
            var url = this.RelativeUrl(string.Format(
                CultureInfo.InvariantCulture,
                ApiRolesPathFmt,
                WebUtility.UrlEncode(NotNull(username, nameof(username)))));

            return this.GetAsync<IEnumerable<string>>(url, cancellationToken);
        }

        /// <inheritdoc />
        public Task AddToRolesAsync(string username, IEnumerable<string> roles, CancellationToken cancellationToken = default)
        {
            var url = this.RelativeUrl(string.Format(
                CultureInfo.InvariantCulture,
                ApiRolesPathFmt,
                WebUtility.UrlEncode(NotNull(username, nameof(username)))));

            return this.SendAsync(HttpMethod.Post, url, roles, cancellationToken);
        }

        /// <inheritdoc />
        public Task RemoveFromRolesAsync(string username, IEnumerable<string> roles, CancellationToken cancellationToken = default)
        {
            var url = this.RelativeUrl(string.Format(
                CultureInfo.InvariantCulture,
                ApiRolesPathFmt,
                WebUtility.UrlEncode(NotNull(username, nameof(username)))));

            return this.SendAsync(HttpMethod.Delete, url, roles, cancellationToken);
        }

#pragma warning disable CA1812
        internal class UserResponse
        {
            public string RegistrationLink { get; set; }
        }
    }
}