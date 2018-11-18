// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.ExternalIdentityProvider
{
    using System;
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

        public OpenIdConnectOptions CreateOptions(IdentityProvider identityProvider)
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

        public IOptionsMonitor<OpenIdConnectOptions> CreateOptionsMonitor(IdentityProvider identityProvider) =>
            new StaticOptionsMonitor(this.CreateOptions(identityProvider));

        private class StaticOptionsMonitor : IOptionsMonitor<OpenIdConnectOptions>
        {
            private static readonly NullDisposable Disposable = new NullDisposable();

            public StaticOptionsMonitor(OpenIdConnectOptions options)
            {
                this.CurrentValue = options;
            }

            public OpenIdConnectOptions CurrentValue { get; }

            public OpenIdConnectOptions Get(string name) => this.CurrentValue;

            public IDisposable OnChange(Action<OpenIdConnectOptions, string> listener) => Disposable;

            private class NullDisposable : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }
    }
}
