using CashFlowTransactions.Infra.CrossCutting.Caching;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CashFlowTransactions.Infra.CrossCutting
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCrossCuttingServices(this IServiceCollection services)
        {
            services.AddMemoryCache(options =>
            {
                options.SizeLimit = 1024; // 1024 entradas
                
                options.ExpirationScanFrequency = TimeSpan.FromMinutes(1); // F
                options.CompactionPercentage = 0.25; // 
            });
            
            // Registrar o serviço Redis como singleton
            services.AddSingleton<RedisCacheService>();
            
            // Registrar o serviço de cache em múltiplas camadas como implementação principal do ICacheService
            services.AddSingleton<ICacheService, MultiLevelCacheService>();
            
            return services;
        }
    }
} 