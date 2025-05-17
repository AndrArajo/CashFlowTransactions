using CashFlowTransactions.Infra.IoC;
using CashFlowTransactions.Infra.Data.Context;
using CashFlowTransactions.Worker;
using Microsoft.EntityFrameworkCore;
using CashFlowTransactions.Domain.Interfaces;
using CashFlowTransactions.Infra.Message.Kafka;
using Microsoft.Extensions.DependencyInjection;
using DotNetEnv;

// Carregar variáveis do arquivo .env se existir (para desenvolvimento local)
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
    .AddEnvironmentVariables() // As variáveis de ambiente tem prioridade
    .AddUserSecrets<Program>(optional: true);

// Configuração do banco de dados usando variáveis de ambiente ou valores das configurações
var dbHost = builder.Configuration["DB_HOST"] ?? Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort = builder.Configuration["DB_PORT"] ?? Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
var dbName = builder.Configuration["POSTGRES_DB"] ?? Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "cashflow";
var dbUser = builder.Configuration["POSTGRES_USER"] ?? Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";
var dbPassword = builder.Configuration["POSTGRES_PASSWORD"] ?? Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";

// Configuração da string de conexão
var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";
builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;

Console.WriteLine($"Configuração do banco de dados: {connectionString}");

// Configuração do Kafka usando variáveis de ambiente ou valores das configurações
var kafkaBootstrapServers = builder.Configuration["KAFKA_BOOTSTRAP_SERVERS"] ?? 
                           Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? 
                           "localhost:29092";
var kafkaTopic = builder.Configuration["KAFKA_TOPIC"] ?? 
                Environment.GetEnvironmentVariable("KAFKA_TOPIC") ?? 
                "transactions";
var kafkaGroupId = builder.Configuration["KAFKA_GROUP_ID"] ?? 
                 Environment.GetEnvironmentVariable("KAFKA_GROUP_ID") ?? 
                 "transaction-consumer-group";
var kafkaAutoOffsetReset = builder.Configuration["KAFKA_AUTO_OFFSET_RESET"] ?? 
                         Environment.GetEnvironmentVariable("KAFKA_AUTO_OFFSET_RESET") ?? 
                         "earliest";

builder.Configuration["Kafka:BootstrapServers"] = kafkaBootstrapServers;
builder.Configuration["Kafka:Topic"] = kafkaTopic;
builder.Configuration["Kafka:GroupId"] = kafkaGroupId;
builder.Configuration["Kafka:AutoOffsetReset"] = kafkaAutoOffsetReset;

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
