﻿// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

#if LIBRARY
using System.ComponentModel.DataAnnotations;

namespace Ironclad.ExternalIdentityProvider.Persistence
#else
namespace Ironclad.Client
#endif
{
    /// <summary>
    /// Represents an identity provider.
    /// </summary>
    public class IdentityProvider
    {
        /// <summary>
        /// Gets or sets the name of the identity provider.
        /// </summary>
        /// <value>The name.</value>
        #if LIBRARY
        [Key]
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
    }
}
