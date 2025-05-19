using System;
using System.Threading.Tasks;

namespace CashFlowTransactions.Infra.CrossCutting.Caching
{
    public interface ICacheService
    {
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task RemoveAsync(string key);
        Task<bool> KeyExistsAsync(string key);
    }
} 