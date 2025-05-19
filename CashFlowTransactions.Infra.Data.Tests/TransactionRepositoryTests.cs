using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CashFlowTransactions.Domain.Entities;
using CashFlowTransactions.Domain.Enums;
using CashFlowTransactions.Infra.Data.Context;
using CashFlowTransactions.Infra.Data.Repositories;
using CashFlowTransactions.Infra.CrossCutting.Caching;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CashFlowTransactions.Infra.Data.Tests
{
    public class TransactionRepositoryTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        private readonly TestCacheService _cacheService;
        private readonly List<Transaction> _testTransactions;
        
        public TransactionRepositoryTests()
        {
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"TransactionDb_{Guid.NewGuid()}")
                .Options;
                
            _cacheService = new TestCacheService();
            
            // Lista de transações para teste
            _testTransactions = new List<Transaction>
            {
                new Transaction { 
                    Id = 1, 
                    Amount = 100.00m, 
                    Type = TransactionType.Credit, 
                    Description = "Salário", 
                    TransactionDate = DateTime.UtcNow.AddDays(-5) 
                },
                new Transaction { 
                    Id = 2, 
                    Amount = 50.00m, 
                    Type = TransactionType.Debit, 
                    Description = "Supermercado", 
                    TransactionDate = DateTime.UtcNow.AddDays(-3) 
                },
                new Transaction { 
                    Id = 3, 
                    Amount = 20.00m, 
                    Type = TransactionType.Debit, 
                    Description = "Taxi", 
                    TransactionDate = DateTime.UtcNow.AddDays(-2) 
                },
                new Transaction { 
                    Id = 4, 
                    Amount = 200.00m, 
                    Type = TransactionType.Credit, 
                    Description = "Freelance", 
                    TransactionDate = DateTime.UtcNow.AddDays(-1) 
                },
                new Transaction { 
                    Id = 5, 
                    Amount = 30.00m, 
                    Type = TransactionType.Debit, 
                    Description = "Farmácia", 
                    TransactionDate = DateTime.UtcNow 
                }
            };
        }
        
        private async Task SeedDatabase()
        {
            using var context = new ApplicationDbContext(_options);
            
            // Limpar banco de dados
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
            
            // Adicionar as transações de teste
            context.Transactions.AddRange(_testTransactions);
            await context.SaveChangesAsync();
        }
        
        [Fact]
        public async Task AddAsync_ShouldAddTransactionAndReturnIt()
        {
            // Arrange
            await SeedDatabase();
            
            using var context = new ApplicationDbContext(_options);
            var repository = new TransactionRepository(context, _cacheService);
            
            var newTransaction = new Transaction(
                amount: 75.50m,
                type: TransactionType.Credit,
                description: "Nova transação",
                origin: "Teste"
            );
            
            // Act
            var result = await repository.AddAsync(newTransaction);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(75.50m, result.Amount);
            Assert.Equal(TransactionType.Credit, result.Type);
            Assert.Equal("Nova transação", result.Description);
            
            // Verificar se realmente foi adicionado no contexto
            var savedTransaction = await context.Transactions.FindAsync(result.Id);
            Assert.NotNull(savedTransaction);
            
            // Verificar se o cache foi invalidado verificando se GetAllAsync busca do banco de dados novamente
            var cacheKey = "transactions_all";
            var exists = await _cacheService.KeyExistsAsync(cacheKey);
            Assert.False(exists); // O cache deve ter sido invalidado
        }
        
        [Fact]
        public async Task GetAllAsync_ShouldReturnAllTransactions()
        {
            // Arrange
            await SeedDatabase();
            
            using var context = new ApplicationDbContext(_options);
            var repository = new TransactionRepository(context, _cacheService);
            
            // Act - Primeira chamada (busca do banco)
            var result1 = await repository.GetAllAsync();
            
            // Extrair algumas informações para verificar na segunda chamada
            var count1 = result1.Count();
            var firstId1 = result1.First().Id;
            
            // Segunda chamada (deve vir do cache)
            var result2 = await repository.GetAllAsync();
            
            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(5, count1);
            Assert.Equal(5, result2.Count());
            Assert.Equal(firstId1, result2.First().Id);
            
            // Verificar se o cache foi utilizado
            var cacheKey = "transactions_all";
            var exists = await _cacheService.KeyExistsAsync(cacheKey);
            Assert.True(exists);
        }
        
        [Fact]
        public async Task GetByIdAsync_WithExistingId_ShouldReturnTransaction()
        {
            // Arrange
            await SeedDatabase();
            
            using var context = new ApplicationDbContext(_options);
            var repository = new TransactionRepository(context, _cacheService);
            
            // Act - Primeira chamada (busca do banco)
            var result1 = await repository.GetByIdAsync(3);
            
            // Segunda chamada (deve vir do cache)
            var result2 = await repository.GetByIdAsync(3);
            
            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(3, result1.Id);
            Assert.Equal(3, result2.Id);
            Assert.Equal(20.00m, result1.Amount);
            Assert.Equal("Taxi", result1.Description);
            
            // Verificar se o cache foi utilizado
            var cacheKey = "transaction_3";
            var exists = await _cacheService.KeyExistsAsync(cacheKey);
            Assert.True(exists);
        }
        
        [Fact]
        public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
        {
            // Arrange
            await SeedDatabase();
            
            using var context = new ApplicationDbContext(_options);
            var repository = new TransactionRepository(context, _cacheService);
            
            // Act
            var result = await repository.GetByIdAsync(999);
            
            // Assert
            Assert.Null(result);
        }
        
        [Fact]
        public async Task GetPaginatedAsync_ShouldReturnPaginatedResults()
        {
            // Arrange
            await SeedDatabase();
            
            using var context = new ApplicationDbContext(_options);
            var repository = new TransactionRepository(context, _cacheService);
            
            var keysAntes = _cacheService.GetAllKeys().ToList();
            
            // Act - Solicitar página 1 com 2 itens por página
            var (items, totalCount) = await repository.GetPaginatedAsync(1, 2);
            
            var keysApos = _cacheService.GetAllKeys().ToList();
            
            var (items2, totalCount2) = await repository.GetPaginatedAsync(1, 2);
            
            // Assert
            Assert.NotNull(items);
            Assert.Equal(2, items.Count());
            Assert.Equal(5, totalCount); // Total de 5 registros
            
            // Verificar ordenação (mais recente primeiro)
            var itemsList = items.ToList();
            Assert.Equal(5, itemsList[0].Id); // Registro mais recente primeiro
            Assert.Equal(4, itemsList[1].Id); // Segundo mais recente
            
            // Verificar se os resultados da segunda chamada são iguais
            Assert.Equal(items.Count(), items2.Count());
            Assert.Equal(totalCount, totalCount2);
            
            // Verificar se o cache contém alguma chave relacionada às transações paginadas
            bool temChavePaginada = _cacheService.GetAllKeys()
                .Any(k => k.StartsWith("transactions_page_"));
                
            Assert.True(temChavePaginada, "Nenhuma chave de paginação encontrada no cache");
        }
        
        [Fact]
        public async Task GetAll_ShouldReturnQueryable()
        {
            // Arrange
            await SeedDatabase();
            
            using var context = new ApplicationDbContext(_options);
            var repository = new TransactionRepository(context, _cacheService);
            
            // Pré-popular o cache com GetAllAsync
            await repository.GetAllAsync();
            
            // Act
            var result = repository.GetAll();
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count());
            
            // Verificar se o cache foi utilizado
            var cacheKey = "transactions_all";
            var exists = await _cacheService.KeyExistsAsync(cacheKey);
            Assert.True(exists);
        }
    }
} 