using CashFlowTransactions.Infra.IoC;
using CashFlowTransactions.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;
using CashFlowTransactions.Domain.Interfaces;
using CashFlowTransactions.Infra.Message.Kafka;
using CashFlowTransactions.Infra.Message.Rabbitmq;
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

// Registrar serviços através do DependencyInjection (que inclui detecção automática de mensageria)
builder.Services.AddDependencyInjection(builder.Configuration);

// Determinar qual consumer registrar baseado no sistema de mensageria detectado
var rabbitmqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? builder.Configuration["RabbitMQ:HostName"];
var messageProvider = Environment.GetEnvironmentVariable("MESSAGE_PROVIDER") ?? builder.Configuration["MessageProvider"];

if (!string.IsNullOrEmpty(rabbitmqHost) || messageProvider?.ToLower() == "rabbitmq")
{
    Console.WriteLine("Usando RabbitMQ como sistema de mensageria");
    builder.Services.AddHostedService<RabbitmqTransactionConsumer>();
}
else
{
    Console.WriteLine("Usando Kafka como sistema de mensageria");
    builder.Services.AddHostedService<KafkaTransactionConsumer>();
}

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var host = builder.Build();

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
