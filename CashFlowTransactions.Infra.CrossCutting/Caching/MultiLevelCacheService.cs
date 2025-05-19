using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CashFlowTransactions.Infra.CrossCutting.Caching
{
    public class MultiLevelCacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly RedisCacheService _redisCache;
        private readonly TimeSpan _memoryCacheExpiration = TimeSpan.FromMinutes(2); // Tempo menor para cache em memória
        private readonly Random _random = new Random();

        public MultiLevelCacheService(IMemoryCache memoryCache, RedisCacheService redisCache)
        {
            _memoryCache = memoryCache;
            _redisCache = redisCache;
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            // Primeira camada: verificar cache em memória
            if (_memoryCache.TryGetValue(key, out T? memoryResult) && memoryResult != null)
            {
                return memoryResult;
            }

            // Segunda camada: verificar cache Redis
            var redisResult = await _redisCache.GetAsync<T>(key);
            if (redisResult != null)
            {
                StoreInMemoryCache(key, redisResult);
                return redisResult;
            }

            var result = await _redisCache.GetOrCreateAsync(key, factory, expiration);
            
            if (result != null)
            {
                StoreInMemoryCache(key, result);
            }
            
            return result;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            if (_memoryCache.TryGetValue(key, out T? memoryResult) && memoryResult != null)
            {
                return memoryResult;
            }

            var redisResult = await _redisCache.GetAsync<T>(key);
            if (redisResult != null)
            {
                StoreInMemoryCache(key, redisResult);
            }

            return redisResult;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            StoreInMemoryCache(key, value);
            
            var jitter = TimeSpan.FromSeconds(_random.Next(30));
            await _redisCache.SetAsync(key, value, expiration.HasValue ? expiration.Value + jitter : null);
        }

        public async Task RemoveAsync(string key)
        {
            // Remover de ambas as camadas
            _memoryCache.Remove(key);
            await _redisCache.RemoveAsync(key);
        }

        public async Task<bool> KeyExistsAsync(string key)
        {
            // Verificar em memória primeiro
            if (_memoryCache.TryGetValue(key, out _))
            {
                return true;
            }

            // Caso contrário, verificar no Redis
            return await _redisCache.KeyExistsAsync(key);
        }

        private void StoreInMemoryCache<T>(string key, T value)
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _memoryCacheExpiration,
                Priority = CacheItemPriority.High,
                Size = 1 // Definir um tamanho para cada entrada (obrigatório quando SizeLimit está configurado)
            };
            
            _memoryCache.Set(key, value, options);
        }
    }
} 