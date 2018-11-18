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

    internal class IdentityProviderAuthenticationHandlerProvider : IAuthenticationHandlerProvider
    {
        private readonly Dictionary<string, IAuthenticationHandler> handlers = new Dictionary<string, IAuthenticationHandler>(StringComparer.Ordinal);

        private readonly IAuthenticationHandlerProvider provider;
        private readonly IAuthenticationSchemeProvider schemes;
        private readonly IStore<IdentityProvider> store;
        private readonly IPostConfigureOptions<OpenIdConnectOptions> configureOptions;
        private readonly ILoggerFactory logger;
        private readonly HtmlEncoder htmlEncoder;
        private readonly UrlEncoder encoder;
        private readonly ISystemClock clock;

        public IdentityProviderAuthenticationHandlerProvider(
            Decorator<IAuthenticationHandlerProvider> decorator,
            Decorator<IAuthenticationSchemeProvider> schemes,
            IStore<IdentityProvider> store,
            IPostConfigureOptions<OpenIdConnectOptions> configureOptions,
            ILoggerFactory logger,
            HtmlEncoder htmlEncoder,
            UrlEncoder encoder,
            ISystemClock clock)
        {
            this.provider = decorator.Instance;
            this.schemes = schemes.Instance;
            this.store = store;
            this.configureOptions = configureOptions;
            this.logger = logger;
            this.htmlEncoder = htmlEncoder;
            this.encoder = encoder;
            this.clock = clock;
        }

        public async Task<IAuthenticationHandler> GetHandlerAsync(HttpContext context, string authenticationScheme)
        {
            if (this.handlers.TryGetValue(authenticationScheme, out var handler))
            {
                return handler;
            }

            var matchedScheme = await this.schemes.GetSchemeAsync(authenticationScheme);
            if (matchedScheme != null)
            {
                handler = await this.provider.GetHandlerAsync(context, authenticationScheme);
                if (handler != null)
                {
                    return handler;
                }

                return null;
            }

            var identityProvider = this.store.Query.SingleOrDefault(provider => provider.Name == authenticationScheme);
            if (identityProvider == null)
            {
                return null;
            }

            var options = new OpenIdConnectOptions
            {
                Authority = identityProvider.Authority,
                ClientId = identityProvider.ClientId,
            };

            options.CallbackPath = identityProvider.CallbackPath ?? options.CallbackPath;

            this.configureOptions.PostConfigure(identityProvider.Name, options);
            var optionsMonitor = new StaticOptionsMonitor(options);

            handler = new OpenIdConnectHandler(optionsMonitor, this.logger, this.htmlEncoder, this.encoder, this.clock);

            var scheme = new AuthenticationScheme(identityProvider.Name, identityProvider.DisplayName, typeof(OpenIdConnectHandler));
            await handler.InitializeAsync(scheme, context);

            this.handlers[authenticationScheme] = handler;

            return handler;
        }
    }
}