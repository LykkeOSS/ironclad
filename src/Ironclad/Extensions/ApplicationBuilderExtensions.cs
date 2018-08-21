// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad
{
    using Ironclad.Application;
    using Ironclad.Data;
    using Ironclad.Data.Maintenance;
    using Marten;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using System.Security.Cryptography.X509Certificates;

    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder InitializeDatabase(this IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                // NOTE (Cameron): Set up ASP.NET Core Identity using Entity Framework (with Postgres).
                var applicationDbContext = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                applicationDbContext.Database.Migrate();
            }

            return app;
        }

        public static IApplicationBuilder SeedDatabase(this IApplicationBuilder app, string authApiSecret)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = serviceScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var store = serviceScope.ServiceProvider.GetRequiredService<IDocumentStore>();
                var logger = serviceScope.ServiceProvider.GetRequiredService<ILogger<SynchronizationManager>>();

                var synchronizationManager = new SynchronizationManager(userManager, roleManager, store, logger);
                synchronizationManager.SynchonizeAdminUserAsync().Wait();
                synchronizationManager.SynchronizeConfigurationAsync(authApiSecret).Wait();

                var compatiabilityManager = new CompatiabilityManager(serviceScope);
                compatiabilityManager.FixInvalidScopesFollowingIdentityServerPackageUpgrade().Wait();
            }

            return app;
        }

        public static IIdentityServerBuilder AddSigningCredential(this IIdentityServerBuilder builder, IConfiguration configuration, ILogger logger)
        {
            string signingCertificatePath = configuration.GetValue<string>("signingCertificatePath");

            if (!string.IsNullOrEmpty(signingCertificatePath))
            {
                logger.LogInformation($"Loading signing certificate '{signingCertificatePath}'.");
                var signingCertificatePassword = configuration.GetValue<string>("signingCertificatePassword");
                var cert = new X509Certificate2(signingCertificatePath, signingCertificatePassword);

                return builder.AddSigningCredential(cert);
            }
            else
            {
                logger.LogWarning($"No signing certificate configured. Temporary dev certificate will be used.");

                return builder.AddDeveloperSigningCredential();
            }
        }
    }
}