// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.ExternalIdentityProvider
{
    using Ironclad.ExternalIdentityProvider.Persistence;
    using Microsoft.AspNetCore.Authentication.OpenIdConnect;
    using Microsoft.Extensions.Options;

    public interface IOpenIdConnectOptionsFactory
    {
        OpenIdConnectOptions CreateOptions(IdentityProvider identityProvider);

        IOptionsMonitor<OpenIdConnectOptions> CreateOptionsMonitor(IdentityProvider identityProvider);
    }
}
