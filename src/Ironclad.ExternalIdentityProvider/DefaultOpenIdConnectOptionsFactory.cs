// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.ExternalIdentityProvider
{
    using Ironclad.ExternalIdentityProvider.Persistence;
    using Microsoft.AspNetCore.Authentication.OpenIdConnect;
    using Microsoft.Extensions.Options;

    public sealed class DefaultOpenIdConnectOptionsFactory : IOpenIdConnectOptionsFactory
    {
        private readonly IPostConfigureOptions<OpenIdConnectOptions> configureOptions;

        public DefaultOpenIdConnectOptionsFactory(IPostConfigureOptions<OpenIdConnectOptions> configureOptions)
        {
            this.configureOptions = configureOptions;
        }

        public OpenIdConnectOptions Create(IdentityProvider identityProvider)
        {
            var options = new OpenIdConnectOptions
            {
                Authority = identityProvider.Authority,
                ClientId = identityProvider.ClientId,
            };

            options.CallbackPath = identityProvider.CallbackPath ?? options.CallbackPath;

            this.configureOptions.PostConfigure(identityProvider.Name, options);

            return options;
        }
    }
}
