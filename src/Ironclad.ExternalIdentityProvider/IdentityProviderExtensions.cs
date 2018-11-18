// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Extensions.DependencyInjection
{
    using Ironclad.ExternalIdentityProvider;
    using Ironclad.ExternalIdentityProvider.Persistence;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.OpenIdConnect;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Options;

    public static class IdentityProviderExtensions
    {
        public static AuthenticationBuilder AddExternalIdentityProviders(this AuthenticationBuilder builder, IStore<IdentityProvider> externalIdentityProviderStore)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<OpenIdConnectOptions>, OpenIdConnectPostConfigureOptions>());
            builder.Services.AddSingleton(externalIdentityProviderStore);
            builder.Services.AddTransient<IOpenIdConnectOptionsFactory, DefaultOpenIdConnectOptionsFactory>();
            builder.Services.AddTransientDecorator<IAuthenticationHandlerProvider, IdentityProviderAuthenticationHandlerProvider>();
            builder.Services.AddTransientDecorator<IAuthenticationSchemeProvider, IdentityProviderAuthenticationSchemeProvider>();

            return builder;
        }
    }
}
