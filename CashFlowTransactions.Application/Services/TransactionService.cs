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
        private readonly ITransactionRepository _repository;

        public TransactionService(ITransactionQueuePublisher publisher, ITransactionRepository repository)
        {
            _publisher = publisher;
            _repository = repository;
        }

        public async Task<Transaction> RegisterAsync(Transaction transaction)
        {
            // Depois publica no Kafka
            await _publisher.PublishAsync(transaction);
            
            return transaction;
        }

        public async Task<IEnumerable<Transaction>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Transaction> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

    }
}
