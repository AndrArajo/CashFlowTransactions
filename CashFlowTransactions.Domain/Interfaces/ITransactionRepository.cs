using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CashFlowTransactions.Domain.Entities;
using CashFlowTransactions.Domain.Enums;

namespace CashFlowTransactions.Domain.Interfaces
{
    public interface ITransactionRepository
    {
        Task<Transaction> AddAsync(Transaction transaction);
        Task<IEnumerable<Transaction>> GetAllAsync();
        Task<Transaction?> GetByIdAsync(int id);
    
    }
}
