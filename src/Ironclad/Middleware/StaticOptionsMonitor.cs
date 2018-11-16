// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Middleware
{
    using System;
    using Microsoft.AspNetCore.Authentication.OpenIdConnect;
    using Microsoft.Extensions.Options;

    public class StaticOptionsMonitor : IOptionsMonitor<OpenIdConnectOptions>
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