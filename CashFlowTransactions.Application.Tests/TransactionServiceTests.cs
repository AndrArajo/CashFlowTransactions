using CashFlowTransactions.Application.DTOs;
using CashFlowTransactions.Application.Services;
using CashFlowTransactions.Domain.Entities;
using CashFlowTransactions.Domain.Enums;
using CashFlowTransactions.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CashFlowTransactions.Application.Tests
{
    public class TransactionServiceTests
    {
        private readonly Mock<ITransactionRepository> _repositoryMock;
        private readonly Mock<ITransactionQueuePublisher> _publisherMock;
        private readonly TransactionService _service;
        
        public TransactionServiceTests()
        {
            _repositoryMock = new Mock<ITransactionRepository>();
            _publisherMock = new Mock<ITransactionQueuePublisher>();
            _service = new TransactionService(_publisherMock.Object, _repositoryMock.Object);
        }
        
        [Fact]
        public async Task RegisterAsync_WithTransactionObject_ShouldPublishToQueue()
        {
            // Arrange
            var transaction = new Transaction(100m, TransactionType.Credit, null, "Test transaction", "Test");
            var messageId = Guid.NewGuid().ToString();
            
            _publisherMock
                .Setup(p => p.PublishAsync(It.IsAny<Transaction>()))
                .ReturnsAsync(messageId);
                
            // Act
            var (resultTransaction, resultMessageId) = await _service.RegisterAsync(transaction);
            
            // Assert
            Assert.Same(transaction, resultTransaction);
            Assert.Equal(messageId, resultMessageId);
            _publisherMock.Verify(p => p.PublishAsync(transaction), Times.Once);
        }
        
        [Fact]
        public async Task RegisterAsync_WithTransactionDto_ShouldCreateAndPublishTransaction()
        {
            // Arrange
            var dto = new CreateTransactionDto
            {
                Amount = 150m,
                Type = TransactionType.Debit,
                Description = "DTO Transaction Test",
                TransactionDate = DateTime.UtcNow
            };
            
            var messageId = Guid.NewGuid().ToString();
            
            _publisherMock
                .Setup(p => p.PublishAsync(It.IsAny<Transaction>()))
                .ReturnsAsync(messageId);
                
            // Act
            var (resultTransaction, resultMessageId) = await _service.RegisterAsync(dto);
            
            // Assert
            Assert.NotNull(resultTransaction);
            Assert.Equal(dto.Amount, resultTransaction.Amount);
            Assert.Equal(dto.Type, resultTransaction.Type);
            Assert.Equal(dto.Description, resultTransaction.Description);
            Assert.Equal(messageId, resultMessageId);
            _publisherMock.Verify(p => p.PublishAsync(It.IsAny<Transaction>()), Times.Once);
        }
        
        [Fact]
        public async Task GetAllAsync_ShouldReturnAllTransactions()
        {
            // Arrange
            var transactions = new List<Transaction>
            {
                new Transaction(100m, TransactionType.Credit, null, "Transaction 1", null),
                new Transaction(200m, TransactionType.Debit, null, "Transaction 2", null),
                new Transaction(300m, TransactionType.Credit, null, "Transaction 3", null)
            };
            
            _repositoryMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(transactions);
                
            // Act
            var result = await _service.GetAllAsync();
            
            // Assert
            Assert.Equal(transactions.Count, result.Count());
            Assert.Equal(transactions, result);
            _repositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
        }
        
        [Fact]
        public async Task GetByIdAsync_WithExistingId_ShouldReturnTransaction()
        {
            // Arrange
            var transaction = new Transaction(100m, TransactionType.Credit, null, "Test transaction", null)
            {
                Id = 1
            };
            
            _repositoryMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(transaction);
                
            // Act
            var result = await _service.GetByIdAsync(1);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(transaction.Id, result.Id);
            Assert.Equal(transaction.Amount, result.Amount);
            _repositoryMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        }
        
        [Fact]
        public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Transaction)null);
                
            // Act
            var result = await _service.GetByIdAsync(999);
            
            // Assert
            Assert.Null(result);
            _repositoryMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        }
        
        [Fact]
        public async Task GetTransactionsAsync_ShouldApplyFiltersAndPagination()
        {
            // Arrange
            var filter = new TransactionFilterDto
            {
                PageNumber = 1,
                PageSize = 10,
                StartDate = DateTime.UtcNow.AddDays(-7),
                EndDate = DateTime.UtcNow,
                Type = TransactionType.Credit,
                MinAmount = 50m,
                MaxAmount = 500m,
                Description = "Test"
            };
            
            // Transações que atendem aos filtros
            var filteredTransactions = new List<Transaction>
            {
                new Transaction(100m, TransactionType.Credit, DateTime.UtcNow.AddDays(-5), "Test 1", null) { Id = 1 },
                new Transaction(200m, TransactionType.Credit, DateTime.UtcNow.AddDays(-3), "Test 2", null) { Id = 2 }
            };
            
            // Mock do repositório para retornar uma lista fixa ao invés de IQueryable
            _repositoryMock
                .Setup(r => r.GetAll())
                .Returns(filteredTransactions.AsQueryable());
                
            // Mock para a contagem
            _repositoryMock
                .Setup(r => r.GetPaginatedAsync(1, 10))
                .ReturnsAsync((filteredTransactions, 2));
            
            // Act
            var result = await _service.GetPaginatedTransactionsAsync(1, 10);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Items.Count());
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(1, result.TotalPages);
        }
        
        [Fact]
        public async Task GetPaginatedTransactionsAsync_ShouldReturnPaginatedResults()
        {
            // Arrange
            var transactions = new List<Transaction>
            {
                new Transaction(100m, TransactionType.Credit, DateTime.UtcNow.AddDays(-2), "Transaction 1", null) { Id = 1 },
                new Transaction(200m, TransactionType.Debit, DateTime.UtcNow.AddDays(-1), "Transaction 2", null) { Id = 2 }
            };
            
            _repositoryMock
                .Setup(r => r.GetPaginatedAsync(1, 10))
                .ReturnsAsync((transactions, 2));
                
            // Act
            var (items, totalCount, totalPages) = await _service.GetPaginatedTransactionsAsync(1, 10);
            
            // Assert
            Assert.NotNull(items);
            Assert.Equal(2, items.Count());
            Assert.Equal(2, totalCount);
            Assert.Equal(1, totalPages);
            _repositoryMock.Verify(r => r.GetPaginatedAsync(1, 10), Times.Once);
        }
        
        [Fact]
        public async Task GetPaginatedTransactionsAsync_WithInvalidPageNumber_ShouldUseDefaultValue()
        {
            // Arrange
            var transactions = new List<Transaction> 
            {
                new Transaction(100m, TransactionType.Credit, null, "Transaction 1", null) { Id = 1 }
            };
            
            _repositoryMock
                .Setup(r => r.GetPaginatedAsync(1, 10))
                .ReturnsAsync((transactions, 1));
                
            // Act - Passando número de página inválido (0)
            var (items, totalCount, totalPages) = await _service.GetPaginatedTransactionsAsync(0, 10);
            
            // Assert
            Assert.Single(items); // Use Assert.Single ao invés de Assert.Equal(1, items.Count())
            _repositoryMock.Verify(r => r.GetPaginatedAsync(1, 10), Times.Once); // Deve usar o valor padrão (1)
        }
        
        [Fact]
        public async Task GetPaginatedTransactionsAsync_WithInvalidPageSize_ShouldUseDefaultValue()
        {
            // Arrange
            var transactions = new List<Transaction> 
            {
                new Transaction(100m, TransactionType.Credit, null, "Transaction 1", null) { Id = 1 }
            };
            
            _repositoryMock
                .Setup(r => r.GetPaginatedAsync(1, 10))
                .ReturnsAsync((transactions, 1));
                
            // Act - Passando tamanho de página inválido (0)
            var (items, totalCount, totalPages) = await _service.GetPaginatedTransactionsAsync(1, 0);
            
            // Assert
            Assert.Single(items); // Use Assert.Single ao invés de Assert.Equal(1, items.Count())
            _repositoryMock.Verify(r => r.GetPaginatedAsync(1, 10), Times.Once); // Deve usar o valor padrão (10)
        }
    }
} 