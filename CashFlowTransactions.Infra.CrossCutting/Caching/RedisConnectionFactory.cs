using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace CashFlowTransactions.Infra.CrossCutting.Caching
{
    public static class RedisConnectionFactory
    {
        private static Lazy<ConnectionMultiplexer> lazyConnection;

        static RedisConnectionFactory()
        {
            lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
            {
                string connectionString;
                
                // Primeiro tentar REDIS_CONNECTION (formato: host:port)
                var redisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION");
                if (!string.IsNullOrEmpty(redisConnection))
                {
                    connectionString = redisConnection;
                }
                else
                {
                    // Fallback para REDIS_HOST e REDIS_PORT separados
                    var host = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
                    var port = Environment.GetEnvironmentVariable("REDIS_PORT") ?? "6379";
                    connectionString = $"{host}:{port}";
                }

                var password = Environment.GetEnvironmentVariable("REDIS_PASSWORD") ?? "redis";

                var configurationOptions = new ConfigurationOptions
                {
                    EndPoints = { connectionString },
                    Password = password,
                    AbortOnConnectFail = false,
                    ConnectRetry = 3
                };

                return ConnectionMultiplexer.Connect(configurationOptions);
            });
        }

        public static ConnectionMultiplexer Connection => lazyConnection.Value;

        public static IDatabase GetDatabase(int db = -1)
        {
            return Connection.GetDatabase(db);
        }
    }
} 