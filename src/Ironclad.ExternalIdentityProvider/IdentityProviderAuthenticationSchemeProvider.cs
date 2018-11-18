// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.ExternalIdentityProvider
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Ironclad.ExternalIdentityProvider.Persistence;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.OpenIdConnect;

    // TODO (Cameron): Need to address the fact that multiple schemes could now be added with the same name.
    // LINK (Cameron): https://github.com/aspnet/HttpAbstractions/blob/master/src/Microsoft.AspNetCore.Authentication.Core/AuthenticationSchemeProvider.cs
    internal class IdentityProviderAuthenticationSchemeProvider : IAuthenticationSchemeProvider
    {
        private readonly IAuthenticationSchemeProvider provider;
        private readonly IStore<IdentityProvider> store;

        public IdentityProviderAuthenticationSchemeProvider(Decorator<IAuthenticationSchemeProvider> decorator, IStore<IdentityProvider> store)
        {
            this.provider = decorator.Instance;
            this.store = store;
        }

        public void AddScheme(AuthenticationScheme scheme) => this.provider.AddScheme(scheme);

        public async Task<IEnumerable<AuthenticationScheme>> GetAllSchemesAsync()
        {
            var registeredSchemes = await this.provider.GetAllSchemesAsync();
            var dynamicSchemes = this.store.Query
                .Select(identityProvider => new AuthenticationScheme(identityProvider.Name, identityProvider.DisplayName, typeof(OpenIdConnectHandler)))
                .AsEnumerable();

            return registeredSchemes.Concat(dynamicSchemes).ToArray();
        }

        public Task<AuthenticationScheme> GetDefaultAuthenticateSchemeAsync() => this.provider.GetDefaultAuthenticateSchemeAsync();

        public Task<AuthenticationScheme> GetDefaultChallengeSchemeAsync() => this.provider.GetDefaultChallengeSchemeAsync();

        public Task<AuthenticationScheme> GetDefaultForbidSchemeAsync() => this.provider.GetDefaultForbidSchemeAsync();

        public Task<AuthenticationScheme> GetDefaultSignInSchemeAsync() => this.provider.GetDefaultSignInSchemeAsync();

        public Task<AuthenticationScheme> GetDefaultSignOutSchemeAsync() => this.provider.GetDefaultSignOutSchemeAsync();

        public Task<IEnumerable<AuthenticationScheme>> GetRequestHandlerSchemesAsync() => this.GetAllSchemesAsync();

        public async Task<AuthenticationScheme> GetSchemeAsync(string name)
        {
            var scheme = await this.provider.GetSchemeAsync(name);
            if (scheme != null)
            {
                return scheme;
            }

            var identityProvider = this.store.Query.SingleOrDefault(provider => provider.Name == name);
            if (identityProvider == null)
            {
                return null;
            }

            return new AuthenticationScheme(identityProvider.Name, identityProvider.DisplayName, typeof(OpenIdConnectHandler));
        }

        public void RemoveScheme(string name) => this.provider.RemoveScheme(name);
    }
}
