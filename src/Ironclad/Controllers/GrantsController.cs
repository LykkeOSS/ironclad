// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Controllers
{
    using System.Linq;
    using System.Threading.Tasks;
    using IdentityServer4.Services;
    using IdentityServer4.Stores;
    using Ironclad.Models;
    using Ironclad.Sdk;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [Authorize]
    [SecurityHeaders]
    public class GrantsController : Controller
    {
        private readonly IIdentityServerInteractionService interaction;
        private readonly IClientStore clients;
        private readonly IResourceStore resources;

        public GrantsController(IIdentityServerInteractionService interaction, IClientStore clients, IResourceStore resources)
        {
            this.interaction = interaction;
            this.clients = clients;
            this.resources = resources;
        }

        [Route("/settings/applications")]
        [HttpGet]
        public async Task<IActionResult> Index() => this.View("Index", await this.BuildViewModelAsync());

        [Route("/settings/applications/revoke")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revoke(string clientId)
        {
            await this.interaction.RevokeUserConsentAsync(clientId);
            return this.RedirectToAction("Index");
        }

        private async Task<GrantsModel> BuildViewModelAsync()
        {
            var model = new GrantsModel();

            var grants = await this.interaction.GetAllUserConsentsAsync();
            foreach (var grant in grants)
            {
                var client = await this.clients.FindClientByIdAsync(grant.ClientId);
                if (client != null)
                {
                    var resources = await this.resources.FindResourcesByScopeAsync(grant.Scopes);

                    var item = new GrantsModel.Grant
                    {
                        ClientId = client.ClientId,
                        ClientName = client.ClientName ?? client.ClientId,
                        ClientLogoUrl = client.LogoUri,
                        ClientUrl = client.ClientUri,
                        Created = grant.CreationTime,
                        Expires = grant.Expiration,
                        IdentityGrantNames = resources.IdentityResources.Select(x => x.DisplayName ?? x.Name).ToArray(),
                        ApiGrantNames = resources.ApiResources.Select(x => x.DisplayName ?? x.Name).ToArray()
                    };

                    model.Grants.Add(item);
                }
            }

            return model;
        }
    }
}