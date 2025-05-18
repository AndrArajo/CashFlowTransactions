using System;
using CashFlowTransactions.Domain.Enums;

namespace CashFlowTransactions.Application.DTOs
{
    public class TransactionDto
    {
        public int Id { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public string? Origin { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateTransactionDto
    {
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public string? Origin { get; set; }
        public DateTime? TransactionDate { get; set; }
    }

    public class UpdateTransactionDto
    {
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public string? Origin { get; set; }
        public DateTime? TransactionDate { get; set; }
    }
} 