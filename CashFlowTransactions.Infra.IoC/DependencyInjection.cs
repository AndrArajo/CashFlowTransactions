using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using CashFlowTransactions.Domain.Interfaces;
using CashFlowTransactions.Infra.Message.Kafka;
using CashFlowTransactions.Infra.Message.Rabbitmq;
using CashFlowTransactions.Infra.Data.Repositories;
using CashFlowTransactions.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;
using System;
using CashFlowTransactions.Application.Services;
using CashFlowTransactions.Infra.CrossCutting;
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

            // Registrar serviços de aplicação
            services.AddScoped<ITransactionService, TransactionService>();

            services.AddCrossCuttingServices();

            // Registrar repositórios
            services.AddScoped<ITransactionRepository, TransactionRepository>();

            // Registrar serviços de mensageria baseado na configuração
            RegisterMessageServices(services, configuration);

            return services;
        }

        private static void RegisterMessageServices(IServiceCollection services, IConfiguration configuration)
        {
            // Detectar automaticamente qual sistema de mensageria usar baseado nas variáveis de ambiente
            var rabbitmqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? configuration["RabbitMQ:HostName"];
            var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? configuration["Kafka:BootstrapServers"];
            var messageProvider = Environment.GetEnvironmentVariable("MESSAGE_PROVIDER") ?? configuration["MessageProvider"];

            // Se RABBITMQ_HOST estiver definido ou MESSAGE_PROVIDER for RabbitMQ, usar RabbitMQ
            if (!string.IsNullOrEmpty(rabbitmqHost) || messageProvider?.ToLower() == "rabbitmq")
            {
                services.AddSingleton<ITransactionQueuePublisher, RabbitmqTransactionPublisher>();
                services.AddSingleton<ITransactionQueueConsumer, RabbitmqTransactionConsumer>();
            }
            // Caso contrário, usar Kafka (padrão)
            else
            {
                services.AddSingleton<ITransactionQueuePublisher, KafkaTransactionPublisher>();
                services.AddSingleton<ITransactionQueueConsumer, KafkaTransactionConsumer>();
            }
        }
    }
} 