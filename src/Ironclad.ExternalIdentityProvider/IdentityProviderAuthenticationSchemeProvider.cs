// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.ExternalIdentityProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Ironclad.ExternalIdentityProvider.Persistence;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.OpenIdConnect;

    // TODO (Cameron): Need to address the fact that multiple schemes could now be added with the same name (somehow).
    // LINK (Cameron): https://github.com/aspnet/HttpAbstractions/blob/master/src/Microsoft.AspNetCore.Authentication.Core/AuthenticationSchemeProvider.cs
#pragma warning disable CA1812
    internal sealed class IdentityProviderAuthenticationSchemeProvider : IAuthenticationSchemeProvider
    {
        private readonly IAuthenticationSchemeProvider schemes;
        private readonly IStore<IdentityProvider> store;

        public IdentityProviderAuthenticationSchemeProvider(Decorator<IAuthenticationSchemeProvider> schemeProvider, IStore<IdentityProvider> store)
        {
            this.schemes = schemeProvider.Instance;
            this.store = store;
        }

        public async Task<IEnumerable<AuthenticationScheme>> GetAllSchemesAsync()
        {
            var registeredSchemes = await this.schemes.GetAllSchemesAsync().ConfigureAwait(false);
            var dynamicSchemes = this.store.Select(identityProvider =>
                new AuthenticationScheme(identityProvider.Name, identityProvider.DisplayName, typeof(OpenIdConnectHandler)))
                ;

            return registeredSchemes.Concat(dynamicSchemes).ToArray();
        }

        public Task<AuthenticationScheme> GetDefaultAuthenticateSchemeAsync() => this.schemes.GetDefaultAuthenticateSchemeAsync();

        public Task<AuthenticationScheme> GetDefaultChallengeSchemeAsync() => this.schemes.GetDefaultChallengeSchemeAsync();

        public Task<AuthenticationScheme> GetDefaultForbidSchemeAsync() => this.schemes.GetDefaultForbidSchemeAsync();

        public Task<AuthenticationScheme> GetDefaultSignInSchemeAsync() => this.schemes.GetDefaultSignInSchemeAsync();

        public Task<AuthenticationScheme> GetDefaultSignOutSchemeAsync() => this.schemes.GetDefaultSignOutSchemeAsync();

        public Task<IEnumerable<AuthenticationScheme>> GetRequestHandlerSchemesAsync() => this.GetAllSchemesAsync();

        public async Task<AuthenticationScheme> GetSchemeAsync(string name)
        {
            var scheme = await this.schemes.GetSchemeAsync(name).ConfigureAwait(false);
            if (scheme != null)
            {
                return scheme;
            }

            var identityProvider = await this.store.SingleOrDefaultAsync(provider => provider.Name == name).ConfigureAwait(false);
            if (identityProvider == null)
            {
                return null;
            }

            return new AuthenticationScheme(identityProvider.Name, identityProvider.DisplayName, typeof(OpenIdConnectHandler));
        }

        public void AddScheme(AuthenticationScheme scheme)
        {
            if (this.store.Any(provider => provider.Name == scheme.Name))
            {
                throw new InvalidOperationException($"Scheme already exists: {scheme.Name}");
            }

            this.schemes.AddScheme(scheme);
        }

        public void RemoveScheme(string name) => this.schemes.RemoveScheme(name);
    }
}
