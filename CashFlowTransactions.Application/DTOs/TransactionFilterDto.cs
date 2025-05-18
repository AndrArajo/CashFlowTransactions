using System;
using CashFlowTransactions.Domain.Enums;

namespace CashFlowTransactions.Application.DTOs
{
    public class TransactionFilterDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public TransactionType? Type { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public string Description { get; set; }
        public string Origin { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string OrderBy { get; set; } = "TransactionDate";
        public bool OrderDescending { get; set; } = true;
    }
} 