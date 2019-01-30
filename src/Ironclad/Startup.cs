// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad
{
    using System;
    using IdentityModel.Client;
    using IdentityServer4.AccessTokenValidation;
    using IdentityServer4.Postgresql.Extensions;
    using IdentityServer4.ResponseHandling;
    using Ironclad.Application;
    using Ironclad.Authorization;
    using Ironclad.Data;
    using Ironclad.Models;
    using Ironclad.Sdk;
    using Ironclad.Services.Email;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc.Routing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;

    public class Startup
    {
        private readonly ILogger<Startup> logger;
        private readonly ILoggerFactory loggerFactory;
        private readonly Settings settings;
        private readonly WebsiteSettings websiteSettings;

        public Startup(ILogger<Startup> logger, ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            this.logger = logger;
            this.loggerFactory = loggerFactory;
            this.settings = configuration.Get<Settings>(options => options.BindNonPublicProperties = true);
            this.websiteSettings = configuration.GetSection("website").Get<WebsiteSettings>(options => options.BindNonPublicProperties = true) ?? new WebsiteSettings();
            this.settings.Validate();

            // HACK (Cameron): Should not be necessary. But is. Needs refactoring.
            this.websiteSettings.RestrictedDomains = this.settings.Idp?.RestrictedDomains ?? Array.Empty<string>();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(this.websiteSettings);

            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(this.settings.Server.Database));

            services.AddIdentity<ApplicationUser, IdentityRole>(
                options =>
                {
                    options.Tokens.ChangePhoneNumberTokenProvider = "Phone";

                    // LINK (Cameron): https://pages.nist.gov/800-63-3/
                    options.Password.RequiredLength = 8;
                    options.Password.RequiredUniqueChars = 0;
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddMvc(options => options.ValueProviderFactories.Add(new SnakeCaseQueryValueProviderFactory()))
                .AddJsonOptions(
                    options =>
                    {
                        options.SerializerSettings.ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() };
                        options.SerializerSettings.Converters.Add(new StringEnumConverter());
                        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    });

            services.AddSingleton<IUrlHelperFactory, SnakeCaseUrlHelperFactory>();

            services.AddIdentityServer(
                options =>
                {
                    options.IssuerUri = this.settings.Server.IssuerUri;
                    options.UserInteraction.LoginUrl = "/signin";
                    options.UserInteraction.LoginReturnUrlParameter = "return_url";
                    options.UserInteraction.LogoutUrl = "/signout";
                    options.UserInteraction.LogoutIdParameter = "logout_id";
                    options.UserInteraction.ConsentUrl = "/settings/applications/consent";
                    options.UserInteraction.ConsentReturnUrlParameter = "return_url";
                    options.UserInteraction.CustomRedirectReturnUrlParameter = "return_url";
                    options.UserInteraction.ErrorUrl = "/signin/error";
                    options.UserInteraction.ErrorIdParameter = "error_id";

                    if (!string.IsNullOrEmpty(this.settings.Api?.Uri))
                    {
                        options.Discovery.CustomEntries.Add("api_uri", this.settings.Api?.Uri);
                    }
                })
                .AddSigningCredentialFromSettings(this.settings, this.loggerFactory)
                .AddConfigurationStore(this.settings.Server.Database)
                .AddOperationalStore()
                .AddAppAuthRedirectUriValidator()
                .AddAspNetIdentity<ApplicationUser>();

            services.AddTransient<IDiscoveryResponseGenerator, CustomDiscoveryResponseGenerator>(
                serviceProvider => new CustomDiscoveryResponseGenerator(serviceProvider, this.settings.Api?.OmitUriForRequestsFrom));

            var authenticationServices = services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication(
                    "token",
                    options =>
                    {
                        options.Authority = this.settings.Api.Authority;
                        options.Audience = this.settings.Api.Audience;
                        options.RequireHttpsMetadata = false;
                    },
                    options =>
                    {
                        options.Authority = this.settings.Api.Authority;
                        options.ClientId = this.settings.Api.ClientId;
                        options.ClientSecret = this.settings.Api.Secret;
                        options.DiscoveryPolicy = new DiscoveryPolicy { ValidateIssuerName = false };
                        options.EnableCaching = true;
                        options.CacheDuration = new TimeSpan(0, 1, 0);
                    })
                .AddExternalIdentityProviders();

            if (this.settings.Idp?.Google.IsValid() == true)
            {
                this.logger.LogInformation("Configuring Google identity provider");
                authenticationServices.AddGoogle(
                    options =>
                    {
                        options.ClientId = this.settings.Idp.Google.ClientId;
                        options.ClientSecret = this.settings.Idp.Google.Secret;
                    });
            }

            // TODO (Cameron): This is a bit messy. I think ultimately this should be configurable inside the application itself.
            if (this.settings.Mail?.IsValid() == true)
            {
                services.AddSingleton<IEmailSender>(
                    new EmailSender(
                        this.settings.Mail.Sender,
                        this.settings.Mail.Host,
                        this.settings.Mail.Port,
                        this.settings.Mail.EnableSsl,
                        this.settings.Mail.Username,
                        this.settings.Mail.Password));
            }
            else
            {
                this.logger.LogWarning("No credentials specified for SMTP. Email will be disabled.");
                services.AddSingleton<IEmailSender>(new NullEmailSender());
            }

            if (this.settings.Server?.DataProtection?.IsValid() == true)
            {
                services.AddDataProtection()
                    .PersistKeysToAzureBlobStorage(new Uri(this.settings.Server.DataProtection.KeyfileUri))
                    .ProtectKeysWithAzureKeyVault(this.settings.Azure.KeyVault.Client, this.settings.Server.DataProtection.KeyId);
            }

            services.AddSingleton<IAuthorizationHandler, ScopeHandler>();
            services.AddSingleton<IAuthorizationHandler, RoleHandler>();

            services.AddAuthorization(
                options =>
                {
                    options.AddPolicy("auth_admin", policy => policy.AddAuthenticationSchemes("token").Requirements.Add(new SystemAdministratorRequirement()));
                    options.AddPolicy("user_admin", policy => policy.AddAuthenticationSchemes("token").Requirements.Add(new UserAdministratorRequirement()));
                });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }

            if (this.settings.Server.RespectXForwardedForHeaders)
            {
                var forwardedHeadersOptions = new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto
                };

                app.UseForwardedHeaders(forwardedHeadersOptions);
                app.UseMiddleware<PathBaseHeaderMiddleware>();
            }

            app.UseMiddleware<AuthCookieMiddleware>();

            if (!string.IsNullOrEmpty(this.settings.Website?.PathBase))
            {
                app.UsePathBase(this.settings.Website.PathBase);
            }

            app.UseStaticFiles();
            app.UseIdentityServer();
            app.UseMvcWithDefaultRoute();
            app.InitializeDatabase().SeedDatabase(this.settings.Api.Secret);
        }
    }
}
