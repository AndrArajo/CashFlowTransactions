# CashFlowTransactions API

API para gerenciamento de transações financeiras, desenvolvida em .NET 8.0 com arquitetura limpa e processamento assíncrono de transações.

## Objetivo do Projeto

O CashFlowTransactions tem como objetivo principal registrar e processar transações financeiras de forma eficiente e escalável. A aplicação:

- Registra transações financeiras (créditos e débitos) com data, valor, descrição e origem
- Processa transações de forma assíncrona utilizando Apache Kafka
- Disponibiliza consultas de transações com filtragem e paginação
- Utiliza cache multinível (memória + Redis) para otimizar o desempenho das consultas
- Implementa padrões de arquitetura limpa para melhor manutenibilidade e escalabilidade

## Executando com Docker

### Pré-requisitos
- Docker
- Docker Compose

### Configuração
1. Clone o repositório
2. Crie um arquivo `.env` na raiz do projeto com as variáveis de ambiente necessárias ou copie os arquivos de configuração:
   ```bash
   cp CashFlowTransactions.API/appsettings.example.json CashFlowTransactions.API/appsettings.json
   cp CashFlowTransactions.Worker/appsettings.example.json CashFlowTransactions.Worker/appsettings.json
   ```
   
   Exemplo de arquivo `.env`:
   ```
   POSTGRES_USER=postgres
   POSTGRES_PASSWORD=postgres
   POSTGRES_DB=cashflow
   KAFKA_TOPIC=transactions
   REDIS_INSTANCE_NAME=CashFlowTransactions:
   ```

3. Ajuste as variáveis de configuração nos arquivos `appsettings.json` conforme necessário (caso não esteja usando o `.env`):
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=postgres;Port=5432;Database=cashflow;Username=postgres;Password=postgres",
       "Redis": "redis:6379"
     },
     "Kafka": {
       "BootstrapServers": "kafka:29092",
       "Topic": "transactions"
     },
     "Redis": {
       "InstanceName": "CashFlowTransactions:"
     }
   }
   ```

### Executando
Para iniciar a aplicação com Docker Compose:
```bash
docker-compose up -d
```

A API estará disponível em: http://localhost:5001/swagger

Para parar a aplicação:
```bash
docker-compose down
```

## Executando com dotnet

### Pré-requisitos
- .NET 8.0 SDK
- PostgreSQL
- Redis
- Apache Kafka

### Configuração
1. Clone o repositório
2. Copie o arquivo `appsettings.example.json` para `appsettings.json`
   ```bash
   cp CashFlowTransactions.API/appsettings.example.json CashFlowTransactions.API/appsettings.json
   cp CashFlowTransactions.Worker/appsettings.example.json CashFlowTransactions.Worker/appsettings.json
   ```
3. Ajuste as variáveis de configuração nos arquivos `appsettings.json` para apontar para suas instâncias locais:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=cashflow;Username=postgres;Password=postgres",
       "Redis": "localhost:6379"
     },
     "Kafka": {
       "BootstrapServers": "localhost:29092",
       "Topic": "transactions"
     },
     "Redis": {
       "InstanceName": "CashFlowTransactions:"
     }
   }
   ```

### Restaurar pacotes e construir
```bash
dotnet restore
dotnet build
```

### Executar migrações do banco de dados
```bash
cd CashFlowTransactions.API
dotnet ef database update
```

### Executar a aplicação
```bash
# Terminal 1 - API
cd CashFlowTransactions.API
dotnet run

# Terminal 2 - Worker
cd CashFlowTransactions.Worker
dotnet run
```

A API estará disponível em: https://localhost:5001/swagger

## Principais Funcionalidades

- Registro de transações financeiras (créditos e débitos)
- Processamento assíncrono de transações via Kafka
- Consulta de transações com filtros por data, tipo, valor e descrição
- Paginação de resultados para melhor performance
- Cache multinível (memória e Redis) para otimização de consultas
- Testes automatizados para todas as camadas da aplicação

## Fluxo de Processamento

1. Transações financeiras são registradas na API e publicadas no tópico Kafka
2. O Worker lê as mensagens do Kafka e processa as transações, salvando-as no banco de dados
3. As consultas são otimizadas pelo cache multinível:
   - Primeiro, verifica-se o cache em memória (mais rápido, menor duração)
   - Se não encontrado, verifica-se o cache no Redis (mais persistente)
   - Se ainda não encontrado, busca-se no banco de dados e armazena-se no cache
4. Estratégia de invalidação de cache garante a consistência dos dados

## Dependências Principais

- ASP.NET Core 8.0
- Entity Framework Core 8.0
- PostgreSQL (via Npgsql)
- Redis (via StackExchange.Redis)
- Apache Kafka (via Confluent.Kafka)
- Swagger/OpenAPI
- xUnit (para testes)
- Moq (para mocks em testes)

## Estrutura da Aplicação

A aplicação segue os princípios de Clean Architecture:

- **Domain**: Entidades, enums e interfaces de repositório
- **Application**: Serviços de aplicação, DTOs e interfaces de serviços
- **Infrastructure**:
  - **Data**: Implementação dos repositórios e contexto EF Core
  - **CrossCutting**: Componentes transversais como cache multinível
  - **Message**: Componentes de mensageria com Kafka
  - **IoC**: Configuração de injeção de dependência
- **API**: Controllers, middlewares e configuração da aplicação
- **Worker**: Serviço para processamento assíncrono de transações

## Testes

A aplicação possui testes automatizados para todas as camadas:

- **Domain.Tests**: Testes para as entidades e regras de domínio
- **Application.Tests**: Testes para os serviços de aplicação
- **Infra.Data.Tests**: Testes para os repositórios

Para executar os testes:

```bash
dotnet test
``` 