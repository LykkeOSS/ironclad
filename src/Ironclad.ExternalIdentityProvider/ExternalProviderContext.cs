using System;
using Ironclad.ExternalIdentityProvider.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Ironclad.ExternalIdentityProvider
{
    public class ExternalProviderContext : DbContext
    {

        public ExternalProviderContext(DbContextOptions<ExternalProviderContext> options)
            : base(options)
        {
        }

        public DbSet<IdentityProvider> ExternalIdentityProviders { get; set; }
       
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {}
    }
}
