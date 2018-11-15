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
    using Xunit;

    public sealed class AuthenticationFixture : IAsyncLifetime
    {
        private readonly string authority;
        private readonly string username;
        private readonly string password;
        private readonly string clientId;
        private readonly string scope;

        public AuthenticationFixture()
        {
            var configuration = new ConfigurationBuilder().AddJsonFile("testsettings.json").Build();

            this.authority = configuration.GetValue<string>("authority") ?? throw new ConfigurationErrorsException("Missing configuration value 'authority'");
            this.username = configuration.GetValue<string>("username") ?? throw new ConfigurationErrorsException("Missing configuration value 'username'");
            this.password = configuration.GetValue<string>("password") ?? throw new ConfigurationErrorsException("Missing configuration value 'password'");
            this.clientId = configuration.GetValue<string>("client_id") ?? throw new ConfigurationErrorsException("Missing configuration value 'client_id'");
            this.scope = configuration.GetValue<string>("scope") ?? throw new ConfigurationErrorsException("Missing configuration value 'scope'");
        }

        public string Authority => this.authority;

        public HttpMessageHandler Handler { get; private set; }

        public async Task InitializeAsync()
        {
            var automation = new BrowserAutomation(this.username, this.password);
            var browser = new Browser(automation);
            var options = new OidcClientOptions
            {
                Authority = this.authority,
                ClientId = this.clientId,
                RedirectUri = $"http://127.0.0.1:{browser.Port}",
                Scope = this.scope,
                FilterClaims = false,
                Browser = browser,
                Policy = new Policy { Discovery = new DiscoveryPolicy { ValidateIssuerName = false } }
            };

            var oidcClient = new OidcClient(options);

            async Task<WaitUntilAvailableResult> WaitUntilAvailable(CancellationToken token)
            {
                try
                {
                    var response = await oidcClient.LoginAsync(new LoginRequest()).ConfigureAwait(false);

                    return WaitUntilAvailableResult.Available(response.AccessToken);
                }
                catch (HttpRequestException)
                {
                }
                catch (InvalidOperationException)
                {
                }

                return WaitUntilAvailableResult.NotAvailable;
            }

            const int maximumWaitUntilAvailableAttempts = 60;
            var timeBetweenWaitUntilAvailableAttempts = TimeSpan.FromSeconds(2);
            var attempt = 0;
            var exit = false;
            string accessToken = null;
            while (
                attempt < maximumWaitUntilAvailableAttempts &&
                !exit)
            {
                var result = await WaitUntilAvailable(default).ConfigureAwait(false);
                if (!ReferenceEquals(result, WaitUntilAvailableResult.NotAvailable))
                {
                    exit = true;
                    accessToken = result.AccessToken;
                }
                else
                {
                    if (attempt != maximumWaitUntilAvailableAttempts - 1)
                    {
                        await Task
                            .Delay(timeBetweenWaitUntilAvailableAttempts, default)
                            .ConfigureAwait(false);
                    }

                    attempt++;
                }
            }

            if (attempt == maximumWaitUntilAvailableAttempts)
            {
                throw new Exception(
                    "The Ironclad instance did not become available in a timely fashion.");
            }

            this.Handler = new TokenHandler(accessToken);
        }

        public Task DisposeAsync()
        {
            this.Handler?.Dispose();
            return Task.CompletedTask;
        }

        private class WaitUntilAvailableResult
        {
            public static readonly WaitUntilAvailableResult NotAvailable = new WaitUntilAvailableResult(null);

            private WaitUntilAvailableResult(string accessToken)
            {
                this.AccessToken = accessToken;
            }

            public string AccessToken { get; }

            public static WaitUntilAvailableResult Available(string accessToken) => new WaitUntilAvailableResult(accessToken);
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