// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.ExternalIdentityProvider.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    public interface IStore<T>
    {
        Task<IEnumerable<T>> AllAsync();

        bool Any(Expression<Func<T, bool>> predicate);

        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

        #pragma warning disable CA1716
        IEnumerable<TResult> Select<TResult>(Func<T, TResult> predicate);

        Task<IdentityProvider> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate);

        IEnumerable<T> Where(Expression<Func<T, bool>> predicate);

        Task AddOrUpdateAsync(string key, T value);

        Task<bool> TryRemoveAsync(string key);
    }
}
