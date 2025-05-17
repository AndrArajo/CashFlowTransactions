using CashFlowTransactions.Application.Services;
using CashFlowTransactions.Domain.Interfaces;
using CashFlowTransactions.Infra.IoC;
using CashFlowTransactions.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using Microsoft.OpenApi.Models;

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

// Configurau00e7u00e3o da string de conexu00e3o
var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";
builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;

Console.WriteLine($"Configurau00e7u00e3o do banco de dados: {connectionString}");

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
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configurar Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CashFlow Transactions API",
        Version = "v1",
        Description = "API para gerenciamento de transações financeiras"
    });
});

// Registrar serviços de aplicação
builder.Services.AddScoped<TransactionService>();

// Registrar as dependências do projeto de IoC
builder.Services.AddDependencyInjection(builder.Configuration);

// Adicionar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CashFlow Transactions API v1"));
}

// Usar HTTPS Redirection apenas se não estivermos rodando em Docker
if (!string.IsNullOrEmpty(builder.Configuration["UseHttpsRedirection"]) && 
    builder.Configuration["UseHttpsRedirection"].ToLower() == "true")
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.UseCors("AllowAll");

app.MapControllers();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
