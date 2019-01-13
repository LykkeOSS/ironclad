// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

#pragma warning disable CA1054

namespace Ironclad.Controllers
{
    using System.Linq;
    using System.Threading.Tasks;
    using IdentityServer4;
    using IdentityServer4.Models;
    using IdentityServer4.Services;
    using IdentityServer4.Stores;
    using Ironclad.Models;
    using Ironclad.Sdk;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    [Authorize]
    [SecurityHeaders]
    public class ConsentController : Controller
    {
        private readonly IIdentityServerInteractionService interaction;
        private readonly IClientStore clientStore;
        private readonly IResourceStore resourceStore;
        private readonly ILogger<ConsentController> logger;

        public ConsentController(IIdentityServerInteractionService interaction, IClientStore clientStore, IResourceStore resourceStore, ILogger<ConsentController> logger)
        {
            this.interaction = interaction;
            this.clientStore = clientStore;
            this.resourceStore = resourceStore;
            this.logger = logger;
        }

        [Route("/settings/applications/consent")]
        [HttpGet]
        public async Task<IActionResult> Index(string returnUrl)
        {
            var model = await this.BuildViewModelAsync(returnUrl);
            if (model != null)
            {
                return this.View("Index", model);
            }

            return this.View("Error");
        }

        [Route("/settings/applications/consent")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ConsentInputModel model)
        {
            var result = await this.ProcessConsent(model);

            if (result.IsRedirect)
            {
                return this.Redirect(result.RedirectUri);
            }

            if (result.HasValidationError)
            {
                this.ModelState.AddModelError(string.Empty, result.ValidationError);
            }

            if (result.ShowView)
            {
                return this.View("Index", result.ViewModel);
            }

            return this.View("Error");
        }

        private async Task<ProcessConsentResult> ProcessConsent(ConsentInputModel model)
        {
            var result = new ProcessConsentResult();

            ConsentResponse grantedConsent = null;

            // user clicked 'no' - send back the standard 'access_denied' response
            if (model.Button == "no")
            {
                grantedConsent = ConsentResponse.Denied;
            }

            // user clicked 'yes' - validate the data
            else if (model.Button == "yes" && model != null)
            {
                // if the user consented to some scope, build the response model
                if (model.ScopesConsented != null && model.ScopesConsented.Any())
                {
                    var scopes = model.ScopesConsented;
                    if (ConsentOptions.EnableOfflineAccess == false)
                    {
                        scopes = scopes.Where(x => x != IdentityServerConstants.StandardScopes.OfflineAccess);
                    }

                    grantedConsent = new ConsentResponse
                    {
                        RememberConsent = model.RememberConsent,
                        ScopesConsented = scopes.ToArray()
                    };
                }
                else
                {
                    result.ValidationError = ConsentOptions.MustChooseOneErrorMessage;
                }
            }
            else
            {
                result.ValidationError = ConsentOptions.InvalidSelectionErrorMessage;
            }

            if (grantedConsent != null)
            {
                // validate return URL is still valid
                var request = await this.interaction.GetAuthorizationContextAsync(model.ReturnUrl);
                if (request == null)
                {
                    return result;
                }

                // communicate outcome of consent back to IdentityServer
                await this.interaction.GrantConsentAsync(request, grantedConsent);

                // indicate that's it OK to redirect back to authorization endpoint
                result.RedirectUri = model.ReturnUrl;
            }
            else
            {
                // we need to redisplay the consent UI
                result.ViewModel = await this.BuildViewModelAsync(model.ReturnUrl, model);
            }

            return result;
        }

        private async Task<ConsentModel> BuildViewModelAsync(string returnUrl, ConsentInputModel model = null)
        {
            var request = await this.interaction.GetAuthorizationContextAsync(returnUrl);
            if (request != null)
            {
                var client = await this.clientStore.FindEnabledClientByIdAsync(request.ClientId);
                if (client != null)
                {
                    var resources = await this.resourceStore.FindEnabledResourcesByScopeAsync(request.ScopesRequested);
                    if (resources != null && (resources.IdentityResources.Any() || resources.ApiResources.Any()))
                    {
                        return this.CreateConsentViewModel(model, returnUrl, client, resources);
                    }
                    else
                    {
                        this.logger.LogError("No scopes matching: {0}", request.ScopesRequested.Aggregate((x, y) => x + ", " + y));
                    }
                }
                else
                {
                    this.logger.LogError("Invalid client id: {0}", request.ClientId);
                }
            }
            else
            {
                this.logger.LogError("No consent request matching request: {0}", returnUrl);
            }

            return null;
        }

        private ConsentModel CreateConsentViewModel(ConsentInputModel inputModel, string returnUrl, Client client, Resources resources)
        {
            var model = new ConsentModel
            {
                RememberConsent = inputModel?.RememberConsent ?? true,
                ScopesConsented = inputModel?.ScopesConsented ?? Enumerable.Empty<string>(),
                ReturnUrl = returnUrl,
                ClientName = client.ClientName ?? client.ClientId,
                ClientUrl = client.ClientUri,
                ClientLogoUrl = client.LogoUri,
                AllowRememberConsent = client.AllowRememberConsent
            };

            model.IdentityScopes = resources.IdentityResources
                .Select(x => this.CreateScopeViewModel(x, model.ScopesConsented.Contains(x.Name) || inputModel == null))
                .ToArray();

            model.ResourceScopes = resources.ApiResources.SelectMany(x => x.Scopes)
                .Select(x => this.CreateScopeViewModel(x, model.ScopesConsented.Contains(x.Name) || inputModel == null))
                .ToArray();

            if (ConsentOptions.EnableOfflineAccess && resources.OfflineAccess)
            {
                model.ResourceScopes = model.ResourceScopes
                    .Union(
                        new[]
                        {
                            this.GetOfflineAccessScope(model.ScopesConsented.Contains(IdentityServerConstants.StandardScopes.OfflineAccess) || inputModel == null)
                        });
            }

            return model;
        }

        private ScopeModel CreateScopeViewModel(IdentityResource identity, bool check)
        {
            return new ScopeModel
            {
                Name = identity.Name,
                DisplayName = identity.DisplayName,
                Description = identity.Description,
                Emphasize = identity.Emphasize,
                Required = identity.Required,
                Checked = check || identity.Required
            };
        }

        private ScopeModel CreateScopeViewModel(Scope scope, bool check)
        {
            return new ScopeModel
            {
                Name = scope.Name,
                DisplayName = scope.DisplayName,
                Description = scope.Description,
                Emphasize = scope.Emphasize,
                Required = scope.Required,
                Checked = check || scope.Required
            };
        }

        private ScopeModel GetOfflineAccessScope(bool check)
        {
            return new ScopeModel
            {
                Name = IdentityServerConstants.StandardScopes.OfflineAccess,
                DisplayName = ConsentOptions.OfflineAccessDisplayName,
                Description = ConsentOptions.OfflineAccessDescription,
                Emphasize = true,
                Checked = check
            };
        }

        private class ProcessConsentResult
        {
            public bool IsRedirect => this.RedirectUri != null;

            public string RedirectUri { get; set; }

            public bool ShowView => this.ViewModel != null;

            public ConsentModel ViewModel { get; set; }

            public bool HasValidationError => this.ValidationError != null;

            public string ValidationError { get; set; }
        }
    }
}