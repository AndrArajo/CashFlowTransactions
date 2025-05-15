using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using CashFlowTransactions.Domain.Interfaces;
using CashFlowTransactions.Infra.Message.Kafka;
using CashFlowTransactions.Infra.Data.Repositories;
using CashFlowTransactions.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;
using System;

namespace CashFlowTransactions.Infra.IoC
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDependencyInjection(this IServiceCollection services, IConfiguration configuration)
        {
            // Registrar banco de dados
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions => 
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                        npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                    });
            });

            // Registrar repositórios
            services.AddScoped<ITransactionRepository, TransactionRepository>();

            // Registrar serviços de mensageria
            services.AddScoped<ITransactionQueuePublisher, KafkaTransactionPublisher>();
      
            services.AddScoped<ITransactionQueueConsumer, KafkaTransactionConsumer>();

            return services;
        }
    }
} 