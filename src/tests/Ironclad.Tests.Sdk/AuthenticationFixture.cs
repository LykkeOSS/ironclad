// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

#pragma warning disable SA1600, CS1591 // not required for this class

namespace Ironclad.Tests.Sdk
{
    using System;
    using System.Configuration;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using IdentityModel.Client;
    using IdentityModel.OidcClient;
    using Microsoft.Extensions.Configuration;

    public sealed class AuthenticationFixture
    {
        private readonly IConfigurationRoot configuration;

        public AuthenticationFixture()
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (string.IsNullOrWhiteSpace(environmentName))
            {
                environmentName = "test";
            }

            this.configuration = new ConfigurationBuilder()
#pragma warning disable CA1308 // Normalize strings to uppercase
                .AddJsonFile($"{environmentName.ToLowerInvariant()}settings.json")
#pragma warning restore CA1308 // Normalize strings to uppercase
                .Build();

            this.Handler = CreateTokenHandler(
                this.Authority = this.configuration.GetValue<string>("authority") ?? throw new ConfigurationErrorsException("Missing configuration value 'authority'"),
                this.configuration.GetValue<string>("username") ?? throw new ConfigurationErrorsException("Missing configuration value 'username'"),
                this.configuration.GetValue<string>("password") ?? throw new ConfigurationErrorsException("Missing configuration value 'password'"),
                this.configuration.GetValue<string>("client_id") ?? throw new ConfigurationErrorsException("Missing configuration value 'client_id'"),
                this.configuration.GetValue<string>("scope") ?? throw new ConfigurationErrorsException("Missing configuration value 'scope'"))
                .GetAwaiter()
                .GetResult();
        }

        public string Authority { get; }

        public HttpMessageHandler Handler { get; }

        private static async Task<HttpMessageHandler> CreateTokenHandler(string authority, string username, string password, string clientId, string scope)
        {
            var automation = new BrowserAutomation(username, password);
            var browser = new Browser(automation);
            var options = new OidcClientOptions
            {
                Authority = authority,
                ClientId = clientId,
                RedirectUri = $"http://127.0.0.1:{browser.Port}",
                Scope = scope,
                FilterClaims = false,
                Browser = browser,
                Policy = new Policy { Discovery = new DiscoveryPolicy { ValidateIssuerName = false, RequireHttps = false } }
            };

            var oidcClient = new OidcClient(options);
            var result = await oidcClient.LoginAsync(new LoginRequest()).ConfigureAwait(false);

            return new TokenHandler(result.AccessToken);
        }

        private sealed class TokenHandler : DelegatingHandler
        {
            private readonly string accessToken;

            public TokenHandler(string accessToken)
                : base(new HttpClientHandler())
            {
                this.accessToken = accessToken;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.accessToken);
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}