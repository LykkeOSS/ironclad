// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Client
{
    /// <summary>
    /// Represents an identity provider summary.
    /// </summary>
    public class IdentityProviderSummary
    {
        /// <summary>
        /// Gets or sets the name of the identity provider.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the display name for the identity provider.
        /// </summary>
        /// <value>The display name.</value>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the authority for the identity provider.
        /// </summary>
        /// <value>The secret.</value>
        public string Authority { get; set; }

        /// <summary>
        /// Gets or sets the client ID for the identity provider.
        /// </summary>
        /// <value>The secret.</value>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="IdentityProvider"/> is enabled.
        /// </summary>
        /// <value>Returns <c>true</c> if enabled; otherwise, <c>false</c>.</value>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the acr_values string for the identity provider.
        /// </summary>
        /// <value>Space-separated string that specifies the acr values.</value>
        public string AcrValues { get; set; }
    }
}
