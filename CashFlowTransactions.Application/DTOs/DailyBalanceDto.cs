using System;
using System.Collections.Generic;

namespace CashFlowTransactions.Application.DTOs
{
    public class DailyBalanceDto
    {
        public DateTime Date { get; set; }
        public decimal TotalCredits { get; set; }
        public decimal TotalDebits { get; set; }
        public decimal NetBalance { get; set; }
        public decimal PreviousDayBalance { get; set; }
        public decimal AccumulatedBalance { get; set; }
        public ICollection<TransactionDto> Transactions { get; set; }
    }

    public class DailyBalanceSummaryDto
    {
        public DateTime Date { get; set; }
        public decimal TotalCredits { get; set; }
        public decimal TotalDebits { get; set; }
        public decimal NetBalance { get; set; }
        public decimal AccumulatedBalance { get; set; }
    }
} 