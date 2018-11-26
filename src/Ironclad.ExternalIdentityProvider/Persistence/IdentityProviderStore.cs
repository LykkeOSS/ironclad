// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.ExternalIdentityProvider.Persistence
{
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.EntityFrameworkCore;

    public sealed class IdentityProviderStore : IStore<IdentityProvider>
    {
        private readonly ExternalProviderContext externalProviderContext;

        public IdentityProviderStore(ExternalProviderContext externalProviderContext)
        {
            this.externalProviderContext = externalProviderContext;
        }

        public IQueryable<IdentityProvider> Query =>
            this.externalProviderContext.ExternalIdentityProviders.AsQueryable();

        public async Task AddOrUpdateAsync(string key, IdentityProvider value)
        {
            var existingItem =
                await this.externalProviderContext.ExternalIdentityProviders.FirstOrDefaultAsync(provider =>
                    provider.Name == key).ConfigureAwait(false);

            if (existingItem == null)
            {
                await this.externalProviderContext.ExternalIdentityProviders.AddAsync(value).ConfigureAwait(false);
            }
            else
            {
                existingItem.Authority = value.Authority;
                existingItem.CallbackPath = value.CallbackPath;
                existingItem.ClientId = value.ClientId;
                existingItem.Name = value.DisplayName;
                existingItem.Name = value.Name;
            }

            await this.externalProviderContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<bool> TryRemoveAsync(string key)
        {
            var item = await this.externalProviderContext.ExternalIdentityProviders
                .FirstOrDefaultAsync(provider => provider.Name == key).ConfigureAwait(false);
            if (item == null)
            {
                return true;
            }

            this.externalProviderContext.Remove(item);
            var itemsChanged = await this.externalProviderContext.SaveChangesAsync().ConfigureAwait(false);

            return itemsChanged == 1;
        }
    }
}