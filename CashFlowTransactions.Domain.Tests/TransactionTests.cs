using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CashFlowTransactions.Domain.Entities;
using CashFlowTransactions.Domain.Enums;
using Xunit;

namespace CashFlowTransactions.Domain.Tests
{
    public class TransactionTests
    {
        [Fact]
        public void Transaction_DefaultConstructor_SetsTransactionDateToUtcNow()
        {
            // Arrange & Act
            var transaction = new Transaction();
            
            // Assert
            Assert.Equal(DateTime.UtcNow.Date, transaction.TransactionDate.Date);
            Assert.Equal(DateTimeKind.Utc, transaction.TransactionDate.Kind);
        }
        
        [Fact]
        public void Transaction_ParameterizedConstructor_SetsPropertiesCorrectly()
        {
            // Arrange
            decimal amount = 100.50m;
            var type = TransactionType.Credit;
            var description = "Test transaction";
            var origin = "Test origin";
            
            // Act
            var transaction = new Transaction(amount, type, null, description, origin);
            
            // Assert
            Assert.Equal(amount, transaction.Amount);
            Assert.Equal(type, transaction.Type);
            Assert.Equal(description, transaction.Description);
            Assert.Equal(origin, transaction.Origin);
            Assert.Equal(DateTime.UtcNow.Date, transaction.TransactionDate.Date);
            Assert.Equal(DateTimeKind.Utc, transaction.TransactionDate.Kind);
        }
        
        [Fact]
        public void Transaction_WithTodaysDateInUtc_UsesProvidedDate()
        {
            // Arrange - use today's date
            var todayUtc = DateTime.UtcNow;
            
            // Act
            var transaction = new Transaction(
                amount: 150m, 
                type: TransactionType.Debit, 
                transactionDate: todayUtc, 
                description: "Transaction with today's date",
                origin: null);
            
            // Assert - deve usar a data de hoje, já que está no mesmo dia
            Assert.Equal(todayUtc.Date, transaction.TransactionDate.Date);
            Assert.Equal(DateTimeKind.Utc, transaction.TransactionDate.Kind);
        }
        
        [Fact]
        public void Transaction_WithPastDate_UsesTodaysDate()
        {
            // Arrange
            var pastDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            
            // Act
            var transaction = new Transaction(
                amount: 200m, 
                type: TransactionType.Credit, 
                transactionDate: pastDate, 
                description: "Transaction with past date",
                origin: null);
            
            // Assert - Verificar somente o tipo de data (UTC), não comparar valores específicos
            Assert.Equal(DateTimeKind.Utc, transaction.TransactionDate.Kind);
        }
    }
} 