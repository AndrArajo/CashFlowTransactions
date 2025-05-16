using CashFlowTransactions.Application.Services;
using CashFlowTransactions.Domain.Interfaces;
using CashFlowTransactions.Infra.IoC;
using CashFlowTransactions.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;
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

var builder = WebApplication.CreateBuilder(args);

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

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

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
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.UseCors("AllowAll");

app.MapControllers();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
