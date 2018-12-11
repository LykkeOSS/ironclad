// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

#pragma warning disable CA1724

namespace Ironclad.Client
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a client.
    /// </summary>
    public class Client
    {
        /// <summary>
        /// Gets or sets the client identifier.
        /// </summary>
        /// <value>The client identifier.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the client name.
        /// </summary>
        /// <value>The client name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the client secret.
        /// </summary>
        /// <value>The client secret.</value>
        public string Secret { get; set; }

        /// <summary>
        /// Gets or sets the allowed CORS origins.
        /// </summary>
        /// <value>The allowed CORS origins.</value>
#pragma warning disable CA2227
        public ICollection<string> AllowedCorsOrigins { get; set; }
#if CLIENT
            = new HashSet<string>();
#endif

        /// <summary>
        /// Gets or sets the redirect URIs.
        /// </summary>
        /// <value>The redirect URIs.</value>
        public ICollection<string> RedirectUris { get; set; }
#if CLIENT
            = new HashSet<string>();
#endif

        /// <summary>
        /// Gets or sets the post logout redirect URIs.
        /// </summary>
        /// <value>The post logout redirect URIs.</value>
        public ICollection<string> PostLogoutRedirectUris { get; set; }
#if CLIENT
            = new HashSet<string>();
#endif

        /// <summary>
        /// Gets or sets the allowed scopes.
        /// </summary>
        /// <value>The allowed scopes.</value>
        public ICollection<string> AllowedScopes { get; set; }
#if CLIENT
            = new HashSet<string>();
#endif

        /// <summary>
        /// Gets or sets the type of the access token.
        /// </summary>
        /// <value>The type of the access token.</value>
        public string AccessTokenType { get; set; }

        /// <summary>
        /// Gets or sets the allowed grant types.
        /// </summary>
        /// <value>The allowed grant types.</value>
        public ICollection<string> AllowedGrantTypes { get; set; }
#if CLIENT
            = new HashSet<string>();
#endif

        /// <summary>
        /// Gets or sets a value indicating whether to allow access tokens via the browser.
        /// </summary>
        /// <value>Returns <c>true</c> if access tokens are allowed via the browser; otherwise, <c>false</c>.</value>
        public bool? AllowAccessTokensViaBrowser { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to allow offline access.
        /// </summary>
        /// <value>Returns <c>true</c> if offline access is allowed; otherwise, <c>false</c>.</value>
        public bool? AllowOfflineAccess { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to require the client secret.
        /// </summary>
        /// <value>Returns <c>true</c> if the client secret is required; otherwise, <c>false</c>.</value>
        public bool? RequireClientSecret { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether PKCE is required.
        /// </summary>
        /// <value>Returns <c>true</c> if PKCE is required; otherwise, <c>false</c>.</value>
        public bool? RequirePkce { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether consent is required.
        /// </summary>
        /// <value>Returns <c>true</c> if consent is required; otherwise, <c>false</c>.</value>
        public bool? RequireConsent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Client"/> is enabled.
        /// </summary>
        /// <value>Returns <c>true</c> if enabled; otherwise, <c>false</c>.</value>
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the local login is allowed for this client. Defaults to <c>true</c>.
        /// </summary>
        /// <value>
        ///   <c>true</c> if local logins are enabled; otherwise, <c>false</c>.
        /// </value>
        public bool? EnableLocalLogin { get; set; }
    }
}