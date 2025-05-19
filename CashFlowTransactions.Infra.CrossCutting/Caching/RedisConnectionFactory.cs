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
                var host = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
                var port = Environment.GetEnvironmentVariable("REDIS_PORT") ?? "6379";
                var password = Environment.GetEnvironmentVariable("REDIS_PASSWORD") ?? "redis";

                var configurationOptions = new ConfigurationOptions
                {
                    EndPoints = { $"{host}:{port}" },
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