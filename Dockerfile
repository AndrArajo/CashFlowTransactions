FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiar arquivos de configuração e solução
COPY *.sln .
# COPY global.json .
COPY CashFlowTransactions.API/*.csproj ./CashFlowTransactions.API/
COPY CashFlowTransactions.Application/*.csproj ./CashFlowTransactions.Application/
COPY CashFlowTransactions.Domain/*.csproj ./CashFlowTransactions.Domain/
COPY CashFlowTransactions.Infra.Data/*.csproj ./CashFlowTransactions.Infra.Data/
COPY CashFlowTransactions.Infra.IoC/*.csproj ./CashFlowTransactions.Infra.IoC/
COPY CashFlowTransactions.Infra.Message/*.csproj ./CashFlowTransactions.Infra.Message/
COPY CashFlowTransactions.Worker/*.csproj ./CashFlowTransactions.Worker/
COPY CashFlowTransactions.Domain.Tests/*.csproj ./CashFlowTransactions.Domain.Tests/
COPY CashFlowTransactions.Application.Tests/*.csproj ./CashFlowTransactions.Application.Tests/
COPY CashFlowTransactions.Infra.Data.Tests/*.csproj ./CashFlowTransactions.Infra.Data.Tests/
COPY CashFlowTransactions.Infra.CrossCutting/*.csproj ./CashFlowTransactions.Infra.CrossCutting/

# Restaurar pacotes
RUN dotnet restore

# Copiar todo o código fonte
COPY . .

# Publicar a API
RUN dotnet publish CashFlowTransactions.API/CashFlowTransactions.API.csproj -c Release -o /app/publish/api

# Publicar o Worker
RUN dotnet publish CashFlowTransactions.Worker/CashFlowTransactions.Worker.csproj -c Release -o /app/publish/worker

# Imagem de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copiar publicações
COPY --from=build /app/publish/api ./api
COPY --from=build /app/publish/worker ./worker

# Copiar script de entrada
COPY docker-entrypoint.sh /app/
RUN chmod +x /app/docker-entrypoint.sh

ENTRYPOINT ["/app/docker-entrypoint.sh"] 