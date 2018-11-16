// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Middleware
{
    using Microsoft.AspNetCore.Authentication.OpenIdConnect;

    public class ExternalIdentityProvider
    {
        public string AuthenticationScheme { get; set; }

        public string DisplayName { get; set; }

        public OpenIdConnectOptions PreConfiguredOptions { get; set; }
    }
}
