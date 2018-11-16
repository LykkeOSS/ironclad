// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Middleware
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading.Tasks;

    public sealed class MemoryExternalIdentityProviderStore : IStore<ExternalIdentityProvider>
    {
        private readonly ConcurrentDictionary<string, ExternalIdentityProvider> contents =
            new ConcurrentDictionary<string, ExternalIdentityProvider>(StringComparer.OrdinalIgnoreCase);

        public IQueryable<ExternalIdentityProvider> Query => this.contents.Values.AsQueryable();

        public Task AddOrUpdateAsync(string key, ExternalIdentityProvider value)
        {
            this.contents.AddOrUpdate(key, value, (k, v) => value);
            return Task.CompletedTask;
        }

        public Task<bool> TryRemoveAsync(string key) => Task.FromResult(this.contents.TryRemove(key, out _));
    }
}
