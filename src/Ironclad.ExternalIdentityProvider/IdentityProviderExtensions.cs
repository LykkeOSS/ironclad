// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Extensions.DependencyInjection
{
    using System;

    using AspNetCore.Authentication;
    using AspNetCore.Authentication.OpenIdConnect;
    using AspNetCore.Builder;
    using EntityFrameworkCore;
    using Extensions;
    using Ironclad.ExternalIdentityProvider;
    using Ironclad.ExternalIdentityProvider.Persistence;
    using Options;

    public static class IdentityProviderExtensions
    {
        public static AuthenticationBuilder AddExternalIdentityProviders(this AuthenticationBuilder builder, Action<DbContextOptionsBuilder> options)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<OpenIdConnectOptions>, OpenIdConnectPostConfigureOptions>());
            builder.Services.AddTransient<IStore<IdentityProvider>, IdentityProviderStore>();
            builder.Services.AddTransient<IOpenIdConnectOptionsFactory, DefaultOpenIdConnectOptionsFactory>();
            builder.Services.AddTransientDecorator<IAuthenticationHandlerProvider, IdentityProviderAuthenticationHandlerProvider>();
            builder.Services.AddTransientDecorator<IAuthenticationSchemeProvider, IdentityProviderAuthenticationSchemeProvider>();
            builder.Services.AddDbContext<ExternalProviderDbContext>(options, ServiceLifetime.Singleton);
            return builder;
        }

        public static IApplicationBuilder InitializeExternalProviderDatabase(this IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                // NOTE (Cameron): Set up ASP.NET Core Identity using Entity Framework (with Postgres).
                var applicationDbContext = serviceScope.ServiceProvider.GetRequiredService<ExternalProviderDbContext>();
                applicationDbContext.Database.Migrate();
            }

            return app;
        }
    }
}
