using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using CashFlowTransactions.Domain.Interfaces;
using CashFlowTransactions.Infra.Message.Kafka;

namespace CashFlowTransactions.Infra.IoC
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDependencyInjection(this IServiceCollection services, IConfiguration configuration)
        {
            // Registrar serviços de mensageria
            services.AddScoped<ITransactionQueuePublisher, KafkaTransactionPublisher>();

            return services;
        }
    }
} 