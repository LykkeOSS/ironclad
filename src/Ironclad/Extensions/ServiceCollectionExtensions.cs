// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Extensions
{
    using System;
    using IdentityServer4.Postgresql.Extensions;
    using Ironclad.Application;
    using Ironclad.Services.Certificates;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureIdentityServer(this IServiceCollection services, IConfiguration configuration, ILogger logger, IHostingEnvironment env)
        {
            var serviceProvider = services.BuildServiceProvider();

            var identityServer = services.AddIdentityServer(options => options.IssuerUri = configuration.GetValue<string>("issuerUri"))
               .AddConfigurationStore(configuration.GetConnectionString("Ironclad"))
               .AddOperationalStore()
               .AddAppAuthRedirectUriValidator()
               .AddAspNetIdentity<ApplicationUser>();

            var certificateConfig = new CertificateConfiguration(
            configuration.GetConnectionString("AzureFileStorageConnectionString"),
            configuration.GetConnectionString("AzureFileStorageShareName"),
            configuration.GetValue<string>("SigningCertificatePath"),
            configuration.GetValue<string>("SigningCertificateThumbprint"));

            var certificateProviderFactory = new CertificateProviderFactory(certificateConfig, serviceProvider);
            var provider = certificateProviderFactory.GetProvider();

            if (provider == null)
            {
                if (env.IsDevelopment())
                {
                    logger.LogWarning($"You are using development environment. A new signing certificate will be generated for you.");
                    identityServer.AddDeveloperSigningCredential();
                }
                else
                {
                    throw new InvalidOperationException("Cannot create certificate provider based on current configuration.");
                }
            }

            var certificate = provider.GetCertificateAsync().Result;

            identityServer.AddSigningCredential(certificate);

            return services;
        }
    }
}
