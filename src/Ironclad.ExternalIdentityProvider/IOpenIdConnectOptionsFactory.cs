// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.ExternalIdentityProvider
{
    using Ironclad.ExternalIdentityProvider.Persistence;
    using Microsoft.AspNetCore.Authentication.OpenIdConnect;

    public interface IOpenIdConnectOptionsFactory
    {
        OpenIdConnectOptions Create(IdentityProvider identityProvider);

    }
}
