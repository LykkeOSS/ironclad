// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.WebApi
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using IdentityServer4.Extensions;
    using Ironclad.Client;
    using Ironclad.Middleware;
    using Microsoft.AspNetCore.Authentication.OpenIdConnect;
    using Microsoft.AspNetCore.Mvc;

    //[Authorize("user_admin")]
    [Route("api/providers")]
    public class ExternalIdentityProvidersController : Controller
    {
        private readonly IStore<ExternalIdentityProvider> store;

        public ExternalIdentityProvidersController(IStore<ExternalIdentityProvider> store)
        {
            this.store = store;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string name, int skip = default, int take = 20)
        {
            skip = Math.Max(0, skip);
            take = take < 0 ? 20 : Math.Min(take, 100);

            var identityProviderQuery = string.IsNullOrEmpty(name)
                ? this.store.Query
                : this.store.Query.Where(identityProvider => identityProvider.AuthenticationScheme.StartsWith(name, StringComparison.OrdinalIgnoreCase));

            var totalSize = identityProviderQuery.Count();
            var identityProviders = identityProviderQuery.OrderBy(identityProvider => identityProvider.AuthenticationScheme).Skip(skip).Take(take).ToList();
            var resources = identityProviders.Select(
                identityProvider =>
                new IdentityProviderResource
                {
                    Url = this.HttpContext.GetIdentityServerRelativeUrl("~/api/providers/" + identityProvider.AuthenticationScheme),
                    Name = identityProvider.AuthenticationScheme,
                    DisplayName = identityProvider.DisplayName,
                });

            var resourceSet = new ResourceSet<IdentityProviderResource>(skip, totalSize, resources);

            return this.Ok(resourceSet);
        }

        [HttpHead("{name}")]
        [HttpGet("{name}")]
        public async Task<IActionResult> Get(string name)
        {
            var identityProvider = this.store.Query.SingleOrDefault(provider => provider.AuthenticationScheme == name);
            if (identityProvider == null)
            {
                return this.NotFound(new { Message = $"Identity provider '{name}' not found" });
            }

            return this.Ok(
                new IdentityProviderResource
                {
                    Url = this.HttpContext.GetIdentityServerRelativeUrl("~/api/providers/" + identityProvider.AuthenticationScheme),
                    Name = identityProvider.AuthenticationScheme,
                    DisplayName = identityProvider.DisplayName,
                    Authority = identityProvider.PreConfiguredOptions.Authority,
                    ClientId = identityProvider.PreConfiguredOptions.ClientId,
                    CallbackPath = identityProvider.PreConfiguredOptions.CallbackPath,
                });
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]IdentityProvider model)
        {
            if (string.IsNullOrEmpty(model.Name))
            {
                return this.BadRequest(new { Message = $"Cannot create an identity provider without a name" });
            }

            if (this.store.Query.Any(provider => provider.AuthenticationScheme == model.Name))
            {
                return this.StatusCode((int)HttpStatusCode.Conflict, new { Message = "Identity provider already exists" });
            }

            var identityProvider = new ExternalIdentityProvider
            {
                AuthenticationScheme = model.Name,
                DisplayName = model.DisplayName,
                PreConfiguredOptions = new OpenIdConnectOptions
                {
                    Authority = model.Authority,
                    ClientId = model.ClientId,
                    CallbackPath = model.CallbackPath,
                },
            };

            await this.store.AddOrUpdateAsync(identityProvider.AuthenticationScheme, identityProvider);

            return this.Created(new Uri(this.HttpContext.GetIdentityServerRelativeUrl("~/api/providers/" + model.Name)), null);
        }

        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string name)
        {
            await this.store.TryRemoveAsync(name);

            return this.Ok();
        }

        private class IdentityProviderResource : IdentityProvider
        {
            public string Url { get; set; }
        }
    }
}
