using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CashFlowTransactions.Infra.CrossCutting.Caching;

namespace CashFlowTransactions.Infra.Data.Tests
{
    /// <summary>
    /// Implementação simples de ICacheService para testes
    /// </summary>
    public class TestCacheService : ICacheService
    {
        private readonly Dictionary<string, object> _cache = new Dictionary<string, object>();

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            if (_cache.TryGetValue(key, out var cachedValue) && cachedValue is T typedValue)
            {
                return typedValue;
            }

            var value = await factory();
            
            // Garantir que o valor seja armazenado no cache, mesmo que seja null
            _cache[key] = value!;
            
            return value;
        }

        public Task<T?> GetAsync<T>(string key)
        {
            if (_cache.TryGetValue(key, out var cachedValue) && cachedValue is T typedValue)
            {
                return Task.FromResult<T?>(typedValue);
            }

            return Task.FromResult<T?>(default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            _cache[key] = value!;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            if (_cache.ContainsKey(key))
            {
                _cache.Remove(key);
            }
            
            return Task.CompletedTask;
        }

        public Task<bool> KeyExistsAsync(string key)
        {
            return Task.FromResult(_cache.ContainsKey(key));
        }
        
        // Método de depuração para ver todas as chaves no cache
        public IEnumerable<string> GetAllKeys()
        {
            return _cache.Keys;
        }
    }
} 