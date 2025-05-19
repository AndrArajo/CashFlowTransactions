using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CashFlowTransactions.Infra.CrossCutting.Caching
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase _db;
        private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(1); 
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        public RedisCacheService()
        {
            _db = RedisConnectionFactory.GetDatabase();
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            // Tentar obter do cache primeiro
            var value = await GetAsync<T>(key);
            if (value != null)
                return value;

            // Chave do bloqueio distribuído
            string lockKey = $"lock:{key}";
            string lockToken = Guid.NewGuid().ToString();
            bool lockAcquired = false;
            
            try
            {
                // Tentar obter o bloqueio distribuído (usando SETNX do Redis)
                // Expiração no bloqueio para evitar deadlocks
                lockAcquired = await _db.StringSetAsync(lockKey, lockToken, TimeSpan.FromSeconds(30), When.NotExists);
                
                if (lockAcquired)
                {
                    // Verificar o cache novamente após obter o bloqueio
                    value = await GetAsync<T>(key);
                    if (value != null)
                        return value;
                        
                    // Executar factory e armazenar no cache
                    value = await factory();
                    await SetAsync(key, value, expiration);
                    return value;
                }
                else
                {
                    // Esperar um pouco e tentar o cache novamente
                    await Task.Delay(500);
                    for (int i = 0; i < 5; i++) // tentar algumas vezes
                    {
                        value = await GetAsync<T>(key);
                        if (value != null)
                            return value;
                        await Task.Delay(200); // pequeno intervalo entre verificações
                    }
                    
                    value = await factory();
                    return value;
                }
            }
            finally
            {
                // Liberar o bloqueio se nós o adquirimos
                // Importante: só liberar se o token for igual ao nosso (para evitar liberar bloqueio de outra instância)
                if (lockAcquired)
                {
                    var script = @"
                        if redis.call('get', KEYS[1]) == ARGV[1] then
                            return redis.call('del', KEYS[1])
                        else
                            return 0
                        end";
                    await _db.ScriptEvaluateAsync(script, new RedisKey[] { lockKey }, new RedisValue[] { lockToken });
                }
            }
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var redisValue = await _db.StringGetAsync(key);
            if (redisValue.IsNullOrEmpty)
                return default;

            try
            {
                string? valueString = redisValue.ToString();
                if (string.IsNullOrEmpty(valueString))
                    return default;
                    
                return JsonConvert.DeserializeObject<T>(valueString);
            }
            catch
            {
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            var redisValue = JsonConvert.SerializeObject(value);
            await _db.StringSetAsync(key, redisValue, expiration ?? _defaultExpiration);
        }

        public async Task RemoveAsync(string key)
        {
            await _db.KeyDeleteAsync(key);
        }

        public async Task<bool> KeyExistsAsync(string key)
        {
            return await _db.KeyExistsAsync(key);
        }
    }
} 