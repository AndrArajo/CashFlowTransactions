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
        IQueryable<Transaction> GetAll();
        Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPaginatedAsync(int pageNumber, int pageSize);
        
        // Método para buscar paginação diretamente do banco de dados, sem usar cache
        Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPaginatedDirectFromDatabaseAsync(int pageNumber, int pageSize);
    }
}
