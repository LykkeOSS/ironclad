﻿// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad
{
    using IdentityModel.Client;
    using IdentityServer4.AccessTokenValidation;
    using IdentityServer4.Postgresql.Extensions;
    using Ironclad.Application;
    using Ironclad.Authorization;
    using Ironclad.Data;
    using Ironclad.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;
    using Serilog;
    using System.Threading;

    public class Startup
    {
        private readonly ILogger<Startup> logger;
        private readonly IConfiguration configuration;

        public Startup(ILogger<Startup> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var migrationsAssembly = typeof(Startup).GetType().Assembly.GetName().Name;

            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(this.configuration.GetConnectionString("Ironclad")));

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

            // TODO (Cameron): This is a bit messy. I think ultimately this should be configurable inside the application itself.
            var mailUsername = this.configuration.GetValue<string>("Mail:Username");
            if (string.IsNullOrEmpty(mailUsername))
            {
                this.logger.LogWarning("No credentials specified for SMTP. Email will be disabled.");
                services.AddSingleton<IEmailSender>(new NullEmailSender());
            }
            else
            {
                services.AddSingleton<IEmailSender>(
                    new EmailSender(
                        this.configuration.GetValue<string>("Mail:Sender"),
                        this.configuration.GetValue<string>("Mail:Host"),
                        this.configuration.GetValue<int>("Mail:Port"),
                        this.configuration.GetValue<bool>("Mail:EnableSSL"),
                        mailUsername,
                        this.configuration.GetValue<string>("Mail:Password")));
            }

            services.AddSingleton<IAuthorizationHandler, ScopeHandler>();
            services.AddSingleton<IAuthorizationHandler, RoleHandler>();

            services.AddMvc()
                .AddJsonOptions(
                    options =>
                    {
                        options.SerializerSettings.ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() };
                        options.SerializerSettings.Converters.Add(new StringEnumConverter());
                        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    });

            services.AddIdentityServer(options => options.IssuerUri = this.configuration.GetValue<string>("issuerUri"))
                .AddDeveloperSigningCredential()
                .AddConfigurationStore(this.configuration.GetConnectionString("Ironclad"))
                .AddOperationalStore()
                .AddAppAuthRedirectUriValidator()
                .AddAspNetIdentity<ApplicationUser>();

            services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddGoogle(
                    options =>
                    {
                        options.ClientId = this.configuration.GetValue<string>("Google-ClientId");
                        options.ClientSecret = this.configuration.GetValue<string>("Google-Secret");
                    })
                .AddIdentityServerAuthentication(
                    "token",
                    options =>
                    {
                        options.Authority = this.configuration.GetValue<string>("authority");
                        options.Audience = $"{this.configuration.GetValue<string>("issuerUri")}/resources";
                        options.RequireHttpsMetadata = false;
                    },
                    options =>
                    {
                        options.Authority = this.configuration.GetValue<string>("authority");
                        options.ClientId = "auth_api";
                        options.ClientSecret = this.configuration.GetValue<string>("Introspection-Secret");
                        options.DiscoveryPolicy = new DiscoveryPolicy { ValidateIssuerName = false };
                    });

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

            if (this.configuration.GetValue<bool>("respectXForwardedForHeaders"))
            {
                var options = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto };
                app.UseForwardedHeaders(options);
            }

            app.UseStaticFiles();
            app.UseIdentityServer();
            app.UseMvcWithDefaultRoute();

            int retryCount = 10;

            for (int i = 1; i <= retryCount; i++)
            {
                try
                {
                    app.InitializeDatabase();
                }
                catch (Npgsql.PostgresException ex)
                {
                    if (ex.Message.Contains("the database system is starting up", System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        this.Wait(retryCount, i, ex);
                    }
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    this.Wait(retryCount, i, ex);
                }
            }

            app.SeedDatabase(this.configuration);
        }

        private void Wait(int retryCount, int i, System.Exception ex)
        {
            Log.Error(ex, $"Will retry {retryCount - i} more times");
            Thread.Sleep(1000);
        }
    }
}
