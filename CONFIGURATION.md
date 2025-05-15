# Configuração do CashFlowTransactions

Este documento explica como configurar as variáveis sensíveis para o projeto CashFlowTransactions.

## Opções de Configuração

Existem várias maneiras de configurar as variáveis sensíveis:

1. **Variáveis de Ambiente**: Recomendado para ambientes de produção
2. **User Secrets**: Recomendado para desenvolvimento local
3. **Arquivos .env**: Para usar com Docker

## Variáveis de Ambiente

As principais variáveis de ambiente necessárias são:

```
# Conexão com o banco de dados
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=cashflow;Username=postgres;Password=postgres

# Configurações do Kafka
Kafka__BootstrapServers=localhost:29092
Kafka__Topic=transactions
Kafka__GroupId=cashflow-group
Kafka__AutoOffsetReset=Earliest
```

No Windows, você pode configurar usando:

```powershell
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=cashflow;Username=postgres;Password=postgres"
```

No Linux/MacOS:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=cashflow;Username=postgres;Password=postgres"
```

## User Secrets (Desenvolvimento)

Para configurar usando user secrets:

```
dotnet user-secrets init --project CashFlowTransactions.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=cashflow;Username=postgres;Password=postgres" --project CashFlowTransactions.API
dotnet user-secrets set "Kafka:BootstrapServers" "localhost:29092" --project CashFlowTransactions.API
dotnet user-secrets set "Kafka:Topic" "transactions" --project CashFlowTransactions.API
```

## Docker

Ao usar Docker, você pode configurar as variáveis de ambiente no docker-compose.yml:

```yaml
services:
  api:
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=cashflow;Username=postgres;Password=postgres
      - Kafka__BootstrapServers=kafka:9092
```

Ou usar um arquivo `.env` com o Docker Compose. 