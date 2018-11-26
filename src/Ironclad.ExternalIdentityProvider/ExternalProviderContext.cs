// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.ExternalIdentityProvider
{
    using Microsoft.EntityFrameworkCore;
    using Persistence;

    public class ExternalProviderContext : DbContext
    {
        public ExternalProviderContext(DbContextOptions<ExternalProviderContext> options)
            : base(options)
        {
        }

        public DbSet<IdentityProvider> ExternalIdentityProviders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }
    }
}
