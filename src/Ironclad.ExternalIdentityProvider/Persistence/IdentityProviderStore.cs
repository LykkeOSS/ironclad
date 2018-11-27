// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.ExternalIdentityProvider.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    using Marten;

    public sealed class IdentityProviderStore : IStore<IdentityProvider>
    {
        private readonly IDocumentStore documentStore;

        public IdentityProviderStore(IDocumentStore documentStore)
        {
            this.documentStore = documentStore;
        }

        public async Task<IEnumerable<IdentityProvider>> AllAsync()
        {
            using (var session = this.documentStore.LightweightSession())
            {
                return await session.Query<IdentityProvider>().ToListAsync().ConfigureAwait(false);
            }
        }

        public bool Any(Expression<Func<IdentityProvider, bool>> predicate)
        {
            using (var session = this.documentStore.LightweightSession())
            {
                return session.Query<IdentityProvider>().Any(predicate);
            }
        }

        public async Task<bool> AnyAsync(Expression<Func<IdentityProvider, bool>> predicate)
        {
            using (var session = this.documentStore.LightweightSession())
            {
                return await session.Query<IdentityProvider>().AnyAsync(predicate).ConfigureAwait(false);
            }
        }

        public IEnumerable<TResult> Select<TResult>(Func<IdentityProvider, TResult> predicate)
        {
            using (var session = this.documentStore.LightweightSession())
            {
               return session.Query<IdentityProvider>().Select(predicate).ToList();
            }
        }

        public IEnumerable<IdentityProvider> Where(Expression<Func<IdentityProvider, bool>> predicate)
        {
            using (var session = this.documentStore.LightweightSession())
            {
                return session.Query<IdentityProvider>().Where(predicate);
            }
        }

        public async Task<IdentityProvider> SingleOrDefaultAsync(Expression<Func<IdentityProvider, bool>> predicate)
        {
            using (var session = this.documentStore.LightweightSession())
            {
                return await session.Query<IdentityProvider>().SingleOrDefaultAsync(predicate).ConfigureAwait(false);
            }
        }

        public async Task AddOrUpdateAsync(string key, IdentityProvider value)
        {
            using (var session = this.documentStore.LightweightSession())
            {
                var existing = await session.Query<IdentityProvider>().FirstOrDefaultAsync(provider => provider.Name == key).ConfigureAwait(false);

                if (existing == null)
                {
                    session.Insert(value);
                }
                else
                {
                    session.Update(value);
                }

                await session.SaveChangesAsync().ConfigureAwait(false);
            }
                    }

        public async Task<bool> TryRemoveAsync(string key)
        {
            using (var session = this.documentStore.LightweightSession())
            {
                session.DeleteWhere<IdentityProvider>(provider => provider.Name == key);
                await session.SaveChangesAsync().ConfigureAwait(false);
            }

            return true;
        }
    }
}