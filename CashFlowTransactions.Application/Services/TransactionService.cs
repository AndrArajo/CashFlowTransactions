using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CashFlowTransactions.Domain.Entities;
using CashFlowTransactions.Domain.Interfaces;

namespace CashFlowTransactions.Application.Services
{
    public class TransactionService
    {
        private readonly ITransactionQueuePublisher _publisher;

        public TransactionService(ITransactionQueuePublisher publisher)
        {
            _publisher = publisher;
        }

        public async Task RegisterAsync(Transaction transaction)
        {
            await _publisher.PublishAsync(transaction); 
        }
    }
}
