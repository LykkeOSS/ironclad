// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

#if PERSISTENCE
namespace Ironclad.ExternalIdentityProvider.Persistence
{
    using Marten.Schema;
#else
namespace Ironclad.Client
{
#endif

    /// <summary>
    /// Represents an identity provider.
    /// </summary>
    public class IdentityProvider
    {
        /// <summary>
        /// Gets or sets the name of the identity provider.
        /// </summary>
        /// <value>The name.</value>
#if PERSISTENCE
        [Identity]
#endif
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
        /// Gets or sets the callback path for the identity provider.
        /// </summary>
        /// <value>The secret.</value>
        public string CallbackPath { get; set; }

        /// <summary>
        /// Gets or sets the acr_values string for the identity provider.
        /// </summary>
        /// <value>Space-separated string that specifies the acr values.</value>
        public string AcrValues { get; set; }

    }
}
