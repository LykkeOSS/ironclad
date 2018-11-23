// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;

namespace Ironclad.ExternalIdentityProvider.Persistence
{
    using System.Linq;
    using System.Threading.Tasks;

    public sealed class IdentityProviderStore : IStore<IdentityProvider>
    {
        private readonly ExternalProviderContext _externalProviderContext;

        public IQueryable<IdentityProvider> Query => this._externalProviderContext.ExternalIdentityProviders.AsQueryable();

        public IdentityProviderStore(ExternalProviderContext externalProviderContext)
        {
            _externalProviderContext = externalProviderContext;
        }
        
        public async Task AddOrUpdateAsync(string key, IdentityProvider value)
        {
            var existingItem =
                await this._externalProviderContext.ExternalIdentityProviders.FirstOrDefaultAsync(provider =>
                    provider.Name == key);

            if (existingItem == null)
            {
                await this._externalProviderContext.ExternalIdentityProviders.AddAsync(value);
            }
            else
            {
                existingItem.Authority = value.Authority;
                existingItem.CallbackPath = value.CallbackPath;
                existingItem.ClientId = value.ClientId;
                existingItem.Name = value.DisplayName;
                existingItem.Name = value.Name;
            }

            await this._externalProviderContext.SaveChangesAsync();
        }

        public async Task<bool> TryRemoveAsync(string key)
        {
            var item = await this._externalProviderContext.ExternalIdentityProviders.FirstOrDefaultAsync(provider => provider.Name == key);
            if (item == null)
            {
                return true;
            }

            this._externalProviderContext.Remove(item);
            var itemsChanged = await this._externalProviderContext.SaveChangesAsync();

            return itemsChanged == 1;
        }
    }
}
