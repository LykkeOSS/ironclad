// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Feature
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using FluentAssertions;
    using IdentityModel;
    using IdentityModel.Client;
    using Ironclad.Client;
    using Ironclad.Tests.Sdk;
    using Newtonsoft.Json.Linq;
    using Xbehave;

    public class GettingCustomClaimsFeature : AuthenticationTest
    {
        private IUsersClient usersClient;
        private IIdentityResourcesClient identityResourcesClient;
        private IApiResourcesClient apiResourcesClient;
        private IClientsClient clientsClient;

        public GettingCustomClaimsFeature(AuthenticationFixture fixture)
            : base(fixture)
        {
        }

        [Background]
        public void Background()
        {
            "Given a users client".x(() => this.usersClient = new UsersHttpClient(this.ApiUri, this.Handler));
            "And an identity resources client".x(() => this.identityResourcesClient = new IdentityResourcesHttpClient(this.ApiUri, this.Handler));
            "And an API resources client".x(() => this.apiResourcesClient = new ApiResourcesHttpClient(this.ApiUri, this.Handler));
            "And a clients client".x(() => this.clientsClient = new ClientsHttpClient(this.ApiUri, this.Handler));
        }

        [Scenario]
        public void CanGetCustomClaims(User user, Client client, AuthorizationResponse response)
        {
            "Given the new scope is added to the authorization server"
                .x(async () => await this.identityResourcesClient.AddIdentityResourceAsync(
                    new IdentityResource
                    {
                        Enabled = true,
                        Name = "amazeballs",
                        DisplayName = "Something something amazing",
                        UserClaims = { "amaze", "balls" }
                    }).ConfigureAwait(false));

            "And an end-user is added to the authorization server _with claim values matching the new scope_"
                .x(async () => await this.usersClient.AddUserAsync(
                    user = new User
                    {
                        Username = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                        Password = "pas$wrod",
                        PhoneNumber = "123",
                        Claims = { { "amaze", "yes" }, { "balls", "no" } },
                    }).ConfigureAwait(false));

            "And an API that requires the claims from the new scope"
                .x(async () => await this.apiResourcesClient.AddApiResourceAsync(
                    new ApiResource
                    {
                        Name = "amazeballs_api",
                        ApiSecret = "secret",
                        DisplayName = "Amazeballs API",
                        UserClaims = { "name", "phone_number", "amaze", "balls" },

                        // NOTE (Cameron): OMG wat?
                        // LINK (Cameron): https://github.com/IdentityServer/IdentityServer4/blob/2.1.1/src/IdentityServer4/Models/ApiResource.cs#L67
                        ApiScopes = { new ApiResource.Scope { Name = "amazeballs_api", UserClaims = { "name", "phone_number", "amaze", "balls" } } },

                        Enabled = true
                    }).ConfigureAwait(false));

            "And a client for that API"
                .x(async () => await this.clientsClient.AddClientAsync(
                    client = new Client
                    {
                        Id = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                        Name = $"{nameof(GettingCustomClaimsFeature)}.{nameof(this.CanGetCustomClaims)} (integration test)",
                        AllowedCorsOrigins = { "http://localhost:5006" },
                        RedirectUris = { "http://localhost:5006/redirect" },
                        AllowedScopes = { "openid", "amazeballs_api" },
                        AllowAccessTokensViaBrowser = true,
                        AllowedGrantTypes = { "implicit" },
                        RequireConsent = false,
                        Enabled = true
                    }).ConfigureAwait(false));

            "When that end-user logs into the authorization server via the client requesting access to the API"
                .x(async (context) =>
                {
                    var url = new RequestUrl(this.Authority + "/connect/authorize")
                        .CreateAuthorizeUrl(client.Id, "id_token token", "openid amazeballs_api", client.RedirectUris.First(), "state", "nonce");
                    var automation = new BrowserAutomation(user.Username, user.Password).Using(context);
                    await automation.NavigateToLoginAsync(url).ConfigureAwait(false);
                    response = await automation.LoginToAuthorizationServerAndCaptureRedirectAsync().ConfigureAwait(false);
                });

            "Then that end-user is authorized to call the API"
                .x(() =>
                {
                    response.IsError.Should().BeFalse();

                    var jwtComponents = response.AccessToken.Split(".", StringSplitOptions.RemoveEmptyEntries);
                    var bytes = Base64Url.Decode(jwtComponents[1]);
                    var json = Encoding.UTF8.GetString(bytes);
                    var claims = JObject.Parse(json);

                    claims.GetValue("phone_number").ToString().Should().Be(user.PhoneNumber);
                    claims.GetValue("amaze").ToString().Should().Be("yes");
                    claims.GetValue("balls").ToString().Should().Be("no");
                });
        }
    }
}
