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
    using Ironclad.ExternalIdentityProvider;
    using Ironclad.ExternalIdentityProvider.Persistence;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using IdentityProvider = Ironclad.ExternalIdentityProvider.Persistence.IdentityProvider;
    using Model = Ironclad.Client.IdentityProvider;

    [Authorize("auth_admin")]
    [Route("api/providers")]
    public class IdentityProvidersController : Controller
    {
        private readonly IStore<IdentityProvider> store;
        private readonly IOpenIdConnectOptionsFactory optionsFactory;

        public IdentityProvidersController(IStore<IdentityProvider> store, IOpenIdConnectOptionsFactory optionsFactory)
        {
            this.store = store;
            this.optionsFactory = optionsFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string name, int skip = default, int take = 20)
        {
            skip = Math.Max(0, skip);
            take = take < 0 ? 20 : Math.Min(take, 100);

            var identityProviderQuery = string.IsNullOrEmpty(name)
                ? (await this.store.AllAsync()).ToList()
                : this.store.Where(identityProvider => identityProvider.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase)).ToList();

            var totalSize = identityProviderQuery.Count();
            var identityProviders = identityProviderQuery.OrderBy(identityProvider => identityProvider.Name).Skip(skip).Take(take).ToList();
            var resources = identityProviders.Select(
                identityProvider =>
                new IdentityProviderSummaryResource
                {
                    Url = this.HttpContext.GetIdentityServerRelativeUrl("~/api/providers/" + identityProvider.Name),
                    Name = identityProvider.Name,
                    DisplayName = identityProvider.DisplayName,
                    Authority = identityProvider.Authority,
                    ClientId = identityProvider.ClientId,
                    Enabled = true,
                });

            var resourceSet = new ResourceSet<IdentityProviderSummaryResource>(skip, totalSize, resources);

            return this.Ok(resourceSet);
        }

        [HttpHead("{name}")]
        [HttpGet("{name}")]
        public async Task<IActionResult> Get(string name)
        {
            var identityProvider = await this.store.SingleOrDefaultAsync(provider => provider.Name == name);
            if (identityProvider == null)
            {
                return this.NotFound(new { Message = $"Identity provider '{name}' not found" });
            }

            return this.Ok(
                    new IdentityProviderResource
                    {
                        Url = this.HttpContext.GetIdentityServerRelativeUrl("~/api/providers/" + identityProvider.Name),
                        Name = identityProvider.Name,
                        DisplayName = identityProvider.DisplayName,
                        Authority = identityProvider.Authority,
                        ClientId = identityProvider.ClientId,
                        CallbackPath = identityProvider.CallbackPath,
                        AcrValues = identityProvider.AcrValues,
                    });
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]Model model)
        {
            if (string.IsNullOrEmpty(model.Name))
            {
                return this.BadRequest(new { Message = "Cannot create an identity devprovider without a name" });
            }

            if (await this.store.AnyAsync(provider => provider.Name == model.Name))
            {
                return this.StatusCode((int)HttpStatusCode.Conflict, new { Message = "Identity provider already exists" });
            }

            if (string.IsNullOrEmpty(model.Authority))
            {
                return this.BadRequest(new { Message = "Cannot create an identity provider without an authority" });
            }

            if (string.IsNullOrEmpty(model.ClientId))
            {
                return this.BadRequest(new { Message = "Cannot create an identity provider without a client ID" });
            }

            var identityProvider = new IdentityProvider
            {
                Name = model.Name,
                DisplayName = model.DisplayName,
                Authority = model.Authority,
                ClientId = model.ClientId,
                CallbackPath = model.CallbackPath,
                AcrValues = model.AcrValues,
            };

            try
            {
                this.optionsFactory.CreateOptions(identityProvider).Validate();
            }
            catch (ArgumentException ex)
            {
                return this.BadRequest(new { Message = ex.Message });
            }

            await this.store.AddOrUpdateAsync(identityProvider.Name, identityProvider);

            return this.Created(new Uri(this.HttpContext.GetIdentityServerRelativeUrl("~/api/providers/" + model.Name)), null);
        }

        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string name)
        {
            await this.store.TryRemoveAsync(name);

            return this.Ok();
        }

        private class IdentityProviderResource : Model
        {
            public string Url { get; set; }
        }

        private class IdentityProviderSummaryResource : IdentityProviderSummary
        {
            public string Url { get; set; }
        }
    }
}
