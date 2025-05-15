using CashFlowTransactions.Infra.IoC;
using CashFlowTransactions.Infra.Data.Context;
using CashFlowTransactions.Worker;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// Adicionar serviços
builder.Services.AddHostedService<Worker>();

// Registrar dependências do IoC
builder.Services.AddDependencyInjection(builder.Configuration);

// Configurar logging
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
