using CashFlowTransactions.Infra.IoC;
using CashFlowTransactions.Infra.Data.Context;
using CashFlowTransactions.Worker;
using Microsoft.EntityFrameworkCore;
using CashFlowTransactions.Domain.Interfaces;
using CashFlowTransactions.Infra.Message.Kafka;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

// Configurar a ordem de prioridade de configuração
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true);

builder.Services.AddDependencyInjection(builder.Configuration);


builder.Services.AddHostedService<KafkaTransactionConsumer>(sp => 
{
    var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new KafkaTransactionConsumer(configuration, scopeFactory);
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var host = builder.Build();

// Aplicar migrações do banco de dados
using (var scope = host.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocorreu um erro ao aplicar as migrações.");
    }
}

host.Run();
