// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.WebApi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using IdentityServer4.Extensions;
    using IdentityServer4.Models;
    using IdentityServer4.Postgresql.Mappers;
    using Ironclad.Client;
    using Marten;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using IdentityServerClient = IdentityServer4.Models.Client;
    using IroncladClient = Ironclad.Client.Client;
    using PostgresClient = IdentityServer4.Postgresql.Entities.Client;

    [Authorize("auth_admin")]
    [Route("api/[controller]")]
    public class ClientsController : Controller
    {
        private readonly IDocumentStore store;

        public ClientsController(IDocumentStore store)
        {
            this.store = store;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string id, int skip = default, int take = 20)
        {
            skip = Math.Max(0, skip);
            take = take < 0 ? 20 : Math.Min(take, 100);

            using (var session = this.store.LightweightSession())
            {
                var clientQuery = string.IsNullOrEmpty(id)
                    ? session.Query<PostgresClient>()
                    : session.Query<PostgresClient>().Where(client => client.ClientId.StartsWith(id, StringComparison.OrdinalIgnoreCase));

                var totalSize = await clientQuery.CountAsync();
                var documents = await clientQuery.OrderBy(client => client.ClientId).Skip(skip).Take(take).ToListAsync();
                var resources = documents.Select(
                    document =>
                    new ClientSummaryResource
                    {
                        Url = this.HttpContext.GetIdentityServerRelativeUrl("~/api/clients/" + document.ClientId),
                        Id = document.ClientId,
                        Name = document.ClientName,
                        Enabled = document.Enabled,
                    });

                var resourceSet = new ResourceSet<ClientSummaryResource>(skip, totalSize, resources);

                return this.Ok(resourceSet);
            }
        }

        [HttpHead("{clientId}")]
        [HttpGet("{clientId}")]
        public async Task<IActionResult> Get(string clientId)
        {
            using (var session = this.store.LightweightSession())
            {
                var document = await session.Query<PostgresClient>().SingleOrDefaultAsync(item => item.ClientId == clientId);
                if (document == null)
                {
                    return this.NotFound(new { Message = $"Client '{clientId}' not found" });
                }

                return this.Ok(
                    new ClientResource
                    {
                        Url = this.HttpContext.GetIdentityServerRelativeUrl("~/api/clients/" + document.ClientId),
                        Id = document.ClientId,
                        Name = document.ClientName,
                        AllowedCorsOrigins = document.AllowedCorsOrigins.Select(item => item.Origin).ToList(),
                        RedirectUris = document.RedirectUris.Select(item => item.RedirectUri).ToList(),
                        PostLogoutRedirectUris =
                            document.PostLogoutRedirectUris.Select(item => item.PostLogoutRedirectUri).ToList(),
                        AllowedScopes = document.AllowedScopes.Select(item => item.Scope).ToList(),
                        AccessTokenType = ((AccessTokenType)document.AccessTokenType).ToString(),
                        AllowedGrantTypes = document.AllowedGrantTypes.Select(item => item.GrantType).ToList(),
                        AllowAccessTokensViaBrowser = document.AllowAccessTokensViaBrowser,
                        AllowOfflineAccess = document.AllowOfflineAccess,
                        RequireClientSecret = document.RequireClientSecret,
                        RequirePkce = document.RequirePkce,
                        RequireConsent = document.RequireConsent,
                        Enabled = document.Enabled,
                        EnableLocalLogin = document.EnableLocalLogin,
                    });
            }
        }

        // NOTE (Cameron): For the time being there will be no server-side validation of clients to ensure that they make sense. That responsibility is left to the user.
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]IroncladClient model)
        {
            if (string.IsNullOrEmpty(model.Id))
            {
                return this.BadRequest(new { Message = $"Cannot create a client without a client ID" });
            }

            var accessTokenType = default(AccessTokenType);
            if (model.AccessTokenType != null && !Enum.TryParse(model.AccessTokenType, out accessTokenType))
            {
                return this.BadRequest(new { Message = $"Access token type '{model.AccessTokenType}' does not exist" });
            }

            var client = new IdentityServerClient { ClientId = model.Id };

            // optional properties
            client.ClientName = model.Name ?? client.ClientName;
            client.ClientSecrets = model.Secret == null ? client.ClientSecrets : new HashSet<Secret> { new Secret(model.Secret.Sha256()) };
            client.AllowedCorsOrigins = model.AllowedCorsOrigins ?? client.AllowedCorsOrigins;
            client.RedirectUris = model.RedirectUris ?? client.RedirectUris;
            client.PostLogoutRedirectUris = model.PostLogoutRedirectUris ?? client.PostLogoutRedirectUris;
            client.AllowedScopes = model.AllowedScopes ?? client.AllowedScopes;
            client.AccessTokenType = model.AccessTokenType == null ? client.AccessTokenType : accessTokenType;
            client.AllowedGrantTypes = model.AllowedGrantTypes ?? client.AllowedGrantTypes;
            client.AllowAccessTokensViaBrowser = model.AllowAccessTokensViaBrowser ?? client.AllowAccessTokensViaBrowser;
            client.AllowOfflineAccess = model.AllowOfflineAccess ?? client.AllowOfflineAccess;
            client.RequireClientSecret = model.RequireClientSecret ?? client.RequireClientSecret;
            client.RequirePkce = model.RequirePkce ?? client.RequirePkce;
            client.RequireConsent = model.RequireConsent ?? client.RequireConsent;
            client.Enabled = model.Enabled ?? client.Enabled;
            client.EnableLocalLogin = model.EnableLocalLogin ?? client.EnableLocalLogin;

            using (var session = this.store.LightweightSession())
            {
                if (session.Query<PostgresClient>().Any(document => document.ClientId == client.ClientId))
                {
                    return this.StatusCode((int)HttpStatusCode.Conflict, new { Message = "Client already exists" });
                }

                session.Insert(client.ToEntity());

                await session.SaveChangesAsync();
            }

            return this.Created(new Uri(this.HttpContext.GetIdentityServerRelativeUrl("~/api/clients/" + model.Id)), null);
        }

        [HttpPut("{clientId}")]
        public async Task<IActionResult> Put(string clientId, [FromBody]IroncladClient model)
        {
            if (string.Equals(clientId, "auth_console", StringComparison.OrdinalIgnoreCase))
            {
                return this.BadRequest(new { Message = $"Cannot modify the authorization console client" });
            }

            using (var session = this.store.LightweightSession())
            {
                var document = await session.Query<PostgresClient>().SingleOrDefaultAsync(item => item.ClientId == clientId);
                if (document == null)
                {
                    return this.NotFound(new { Message = $"Client '{clientId}' not found" });
                }

                var accessTokenType = default(AccessTokenType);
                if (model.AccessTokenType != null && !Enum.TryParse(model.AccessTokenType, out accessTokenType))
                {
                    return this.BadRequest(new { Message = $"Access token type '{model.AccessTokenType}' does not exist" });
                }

                // NOTE (Cameron): Because of the mapping/conversion unknowns we rely upon the Postgres integration to perform that operation which is why we do this...
                var client = new IdentityServerClient
                {
                    AllowedCorsOrigins = model.AllowedCorsOrigins,
                    RedirectUris = model.RedirectUris,
                    PostLogoutRedirectUris = model.PostLogoutRedirectUris,
                    AllowedScopes = model.AllowedScopes,
                    AllowedGrantTypes = model.AllowedGrantTypes,
                };

                client.AccessTokenType = model.AccessTokenType == null ? client.AccessTokenType : accessTokenType;

                // NOTE (Cameron): If the secret is updated we want to add the new secret...
                if (!string.IsNullOrEmpty(model.Secret))
                {
                    client.ClientSecrets = new List<Secret> { new Secret(model.Secret.Sha256()) };
                }

                var entity = client.ToEntity();

                // update properties (everything supported is an optional update eg. if null is passed we will not update)
                document.ClientName = model.Name ?? document.ClientName;
                document.AllowedCorsOrigins = entity.AllowedCorsOrigins ?? document.AllowedCorsOrigins;
                document.RedirectUris = entity.RedirectUris ?? document.RedirectUris;
                document.PostLogoutRedirectUris = entity.PostLogoutRedirectUris ?? document.PostLogoutRedirectUris;
                document.AllowedScopes = entity.AllowedScopes ?? document.AllowedScopes;
                document.AccessTokenType = model.AccessTokenType == null ? document.AccessTokenType : entity.AccessTokenType;
                document.AllowedGrantTypes = entity.AllowedGrantTypes ?? document.AllowedGrantTypes;
                document.AllowAccessTokensViaBrowser = model.AllowAccessTokensViaBrowser ?? document.AllowAccessTokensViaBrowser;
                document.AllowOfflineAccess = model.AllowOfflineAccess ?? document.AllowOfflineAccess;
                document.RequireClientSecret = model.RequireClientSecret ?? document.RequireClientSecret;
                document.RequirePkce = model.RequirePkce ?? document.RequirePkce;
                document.RequireConsent = model.RequireConsent ?? document.RequireConsent;
                document.Enabled = model.Enabled ?? document.Enabled;
                document.EnableLocalLogin = model.EnableLocalLogin ?? document.EnableLocalLogin;

                if (!string.IsNullOrEmpty(model.Secret))
                {
                    document.ClientSecrets.Add(entity.ClientSecrets.First());
                }

                session.Update(document);

                await session.SaveChangesAsync();
            }

            return this.Ok();
        }

        [HttpDelete("{clientId}")]
        public async Task<IActionResult> Delete(string clientId)
        {
            if (string.Equals(clientId, "auth_console", StringComparison.OrdinalIgnoreCase))
            {
                return this.BadRequest(new { Message = $"Cannot remove the authorization console client" });
            }

            using (var session = this.store.LightweightSession())
            {
                session.DeleteWhere<PostgresClient>(document => document.ClientId == clientId);
                await session.SaveChangesAsync();
            }

            return this.Ok();
        }

#pragma warning disable CA1034, CA1056
        public class ClientResource : IroncladClient
        {
            public string Url { get; set; }
        }

        private class ClientSummaryResource : ClientSummary
        {
            public string Url { get; set; }
        }
    }
}