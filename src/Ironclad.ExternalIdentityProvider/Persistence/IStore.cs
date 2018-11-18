// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.ExternalIdentityProvider.Persistence
{
    using System.Linq;
    using System.Threading.Tasks;

    public interface IStore<T>
    {
        IQueryable<T> Query { get; }

        Task AddOrUpdateAsync(string key, T value);

        Task<bool> TryRemoveAsync(string key);
    }
}
