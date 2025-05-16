using CashFlowTransactions.Infra.IoC;
using CashFlowTransactions.Infra.Data.Context;
using CashFlowTransactions.Worker;
using Microsoft.EntityFrameworkCore;
using CashFlowTransactions.Domain.Interfaces;
using CashFlowTransactions.Infra.Message.Kafka;
using Microsoft.Extensions.DependencyInjection;
using DotNetEnv;

// Carregar variáveis do arquivo .env se existir
if (File.Exists(".env"))
{
    Env.Load();
}
else if (File.Exists("../.env"))
{
    Env.Load("../.env");
}

var builder = Host.CreateApplicationBuilder(args);

// Configurar a ordem de prioridade de configuração
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true);

if (Env.GetBool("LOADED", false))
{
    // Configuração do banco de dados
    builder.Configuration["ConnectionStrings:DefaultConnection"] = 
        $"Host={Env.GetString("DB_HOST", "localhost")};" +
        $"Port={Env.GetString("DB_PORT", "5432")};" +
        $"Database={Env.GetString("POSTGRES_DB", "cashflow")};" +
        $"Username={Env.GetString("POSTGRES_USER", "postgres")};" +
        $"Password={Env.GetString("POSTGRES_PASSWORD", "postgres")}";

    // Configuração do Kafka
    builder.Configuration["Kafka:BootstrapServers"] = Env.GetString("KAFKA_BOOTSTRAP_SERVERS", "localhost:29092");
    builder.Configuration["Kafka:Topic"] = Env.GetString("KAFKA_TOPIC", "transactions");
    builder.Configuration["Kafka:GroupId"] = Env.GetString("KAFKA_GROUP_ID", "transaction-consumer-group");
    builder.Configuration["Kafka:AutoOffsetReset"] = Env.GetString("KAFKA_AUTO_OFFSET_RESET", "earliest");
}

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
