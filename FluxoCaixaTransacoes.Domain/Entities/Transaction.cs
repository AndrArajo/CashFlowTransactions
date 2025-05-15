using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CashFlowTransactions.Domain.Enums;
using CashFlowTransactions.Domain.Interfaces;

namespace CashFlowTransactions.Domain.Entities
{
    public sealed class Transaction
    {
        public int Id { get; private set; }
        public string? Description { get; private set; }
        public decimal Amount { get; private set; }
        public TransactionType Type { get; private set; }
        public string? Origin { get; private set; }
        public DateTime TransactionDate { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        public Transaction(decimal amount, TransactionType type, DateTime transactionDate, string? description = null, string? origin = null)
        {
            Amount          = amount;
            Type            = type;
            TransactionDate = transactionDate;
            Description     = description;
            Origin          = origin;
            CreatedAt       = DateTime.UtcNow;
        }
    }
}
