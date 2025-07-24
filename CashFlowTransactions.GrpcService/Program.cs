using CashFlowTransactions.GrpcService.Services;
using CashFlowTransactions.Infra.IoC;
using CashFlowTransactions.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;
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

var builder = WebApplication.CreateBuilder(args);

// Configurar a ordem de prioridade de configuração
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables() // As variáveis de ambiente têm prioridade
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

// Add services to the container.
builder.Services.AddGrpc();

// Registrar as dependências do projeto de IoC
builder.Services.AddDependencyInjection(builder.Configuration);

var app = builder.Build();

// Aplicar migrações ao iniciar
using (var scope = app.Services.CreateScope())
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

Console.WriteLine("Iniciando o serviço gRPC...");

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGrpcService<TransactionService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
