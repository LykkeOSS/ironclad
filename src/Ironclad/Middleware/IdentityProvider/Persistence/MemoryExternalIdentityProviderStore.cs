// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Middleware
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading.Tasks;

    public sealed class MemoryExternalIdentityProviderStore : IStore<IdentityProvider>
    {
        private readonly ConcurrentDictionary<string, IdentityProvider> contents =
            new ConcurrentDictionary<string, IdentityProvider>(StringComparer.OrdinalIgnoreCase);

        public IQueryable<IdentityProvider> Query => this.contents.Values.AsQueryable();

        public Task AddOrUpdateAsync(string key, IdentityProvider value)
        {
            this.contents.AddOrUpdate(key, value, (k, v) => value);
            return Task.CompletedTask;
        }

        public Task<bool> TryRemoveAsync(string key) => Task.FromResult(this.contents.TryRemove(key, out _));
    }
}
