using Microsoft.Extensions.DependencyInjection;
using System;

namespace CashFlowTransactions.Infra.CrossCutting.IoC
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCrossCuttingServices(this IServiceCollection services)
        {
            // Aqui serão adicionados componentes de infraestrutura transversais
            // como logging, autenticação, autorização, validação, etc.
            
            return services;
        }
    }
} 