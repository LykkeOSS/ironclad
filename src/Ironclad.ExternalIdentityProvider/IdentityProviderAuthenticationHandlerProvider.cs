// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.ExternalIdentityProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using Ironclad.ExternalIdentityProvider.Persistence;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.OpenIdConnect;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

#pragma warning disable CA1812
    internal class IdentityProviderAuthenticationHandlerProvider : IAuthenticationHandlerProvider
    {
        private readonly Dictionary<string, IAuthenticationHandler> cachedHandlers = new Dictionary<string, IAuthenticationHandler>(StringComparer.Ordinal);

        private readonly IAuthenticationHandlerProvider handlers;
        private readonly IAuthenticationSchemeProvider schemes;
        private readonly IStore<IdentityProvider> store;
        private readonly IOpenIdConnectOptionsFactory optionsFactory;
        private readonly ILoggerFactory logger;
        private readonly HtmlEncoder htmlEncoder;
        private readonly UrlEncoder encoder;
        private readonly ISystemClock clock;

        public IdentityProviderAuthenticationHandlerProvider(
            Decorator<IAuthenticationHandlerProvider> handlerProvider,
            Decorator<IAuthenticationSchemeProvider> schemeProvider,
            IStore<IdentityProvider> store,
            IOpenIdConnectOptionsFactory optionsFactory,
            ILoggerFactory logger,
            HtmlEncoder htmlEncoder,
            UrlEncoder encoder,
            ISystemClock clock)
        {
            this.handlers = handlerProvider.Instance;
            this.schemes = schemeProvider.Instance;
            this.store = store;
            this.optionsFactory = optionsFactory;
            this.logger = logger;
            this.htmlEncoder = htmlEncoder;
            this.encoder = encoder;
            this.clock = clock;
        }

        public async Task<IAuthenticationHandler> GetHandlerAsync(HttpContext context, string authenticationScheme)
        {
            if (this.cachedHandlers.TryGetValue(authenticationScheme, out var handler))
            {
                return handler;
            }

            var scheme = await this.schemes.GetSchemeAsync(authenticationScheme).ConfigureAwait(false);
            if (scheme != null)
            {
                handler = await this.handlers.GetHandlerAsync(context, authenticationScheme).ConfigureAwait(false);
                if (handler != null)
                {
                    return handler;
                }

                return null;
            }

            var identityProvider = await this.store.SingleOrDefaultAsync(provider => provider.Name == authenticationScheme).ConfigureAwait(false);
            if (identityProvider == null)
            {
                return null;
            }

            var optionsMonitor = this.optionsFactory.CreateOptionsMonitor(identityProvider);

            handler = new OpenIdConnectHandler(optionsMonitor, this.logger, this.htmlEncoder, this.encoder, this.clock);

            await handler.InitializeAsync(new AuthenticationScheme(identityProvider.Name, identityProvider.DisplayName, typeof(OpenIdConnectHandler)), context)
                .ConfigureAwait(false);

            this.cachedHandlers[authenticationScheme] = handler;

            return handler;
        }
    }
}