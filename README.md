# CashFlowTransactions API

API para gerenciamento de transações financeiras, desenvolvida em .NET 8.0 com arquitetura limpa e processamento assíncrono de transações.

## Objetivo do Projeto

O CashFlowTransactions tem como objetivo principal registrar e processar transações financeiras de forma eficiente e escalável. A aplicação:

- Registra transações financeiras (créditos e débitos) com data, valor, descrição e origem
- Processa transações de forma assíncrona utilizando Apache Kafka
- Disponibiliza consultas de transações com filtragem e paginação
- Utiliza cache multinível (memória + Redis) para otimizar o desempenho das consultas
- Implementa padrões de arquitetura limpa para melhor manutenibilidade e escalabilidade

## CI/CD

O projeto utiliza GitHub Actions para integração contínua e entrega contínua:

- **Integração Contínua**: Testes automatizados são executados em cada push e pull request
- **Entrega Contínua**: Builds bem-sucedidos na branch main são automaticamente publicados no DockerHub
- **Imagem Docker**: Disponível em andrarajo/cashflow-dailybalance
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
2. Crie um arquivo `.env` na raiz do projeto
3. Copie os arquivos de configuração:
   ```bash
   cp CashFlowTransactions.API/appsettings.example.json CashFlowTransactions.API/appsettings.json
   cp CashFlowTransactions.Worker/appsettings.example.json CashFlowTransactions.Worker/appsettings.json
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

## Estrutura de Pastas

- **CashFlowTransactions.Domain**: 
  - Entidades e regras de negócio
  - Interfaces de repositórios
  - Enums e definições de domínio

- **CashFlowTransactions.Application**: 
  - Serviços de aplicação
  - DTOs e interfaces
  - Mapeamentos
  - Serviços agendados

- **CashFlowTransactions.Infra.Data**: 
  - Implementações de repositórios
  - Contexto de banco de dados 
  - Configurações de entidades
  - Migrações

- **CashFlowTransactions.Infra.CrossCutting**: 
  - Serviços transversais
  - Cache
  - Logging
  - Utilidades

- **CashFlowTransactions.Infra.IoC**: 
  - Configuração de injeção de dependências
  - Registro de serviços

- **CashFlowTransactions.API**: 
  - Controllers
  - Configuração da aplicação
  - Middlewares
  - Filtros

- **CashFlowTransactions.*.Tests**: 
  - Testes unitários
  - Mocks e fixtures

## Testes

A aplicação possui testes automatizados para todas as camadas:

- **Domain.Tests**: Testes para as entidades e regras de domínio
- **Application.Tests**: Testes para os serviços de aplicação
- **Infra.Data.Tests**: Testes para os repositórios

Para executar os testes:

```bash
dotnet test
``` 