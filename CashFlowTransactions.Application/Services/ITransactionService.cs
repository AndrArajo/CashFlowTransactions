using System.Collections.Generic;
using System.Threading.Tasks;
using CashFlowTransactions.Application.DTOs;
using CashFlowTransactions.Domain.Entities;

namespace CashFlowTransactions.Application.Services
{
    public interface ITransactionService
    {
        Task<(Transaction transaction, string messageId)> RegisterAsync(Transaction transaction);
        Task<(Transaction transaction, string messageId)> RegisterAsync(CreateTransactionDto createDto);
        Task<IEnumerable<Transaction>> GetAllAsync();
        Task<Transaction?> GetByIdAsync(int id);
        Task<PaginatedResponseDto<TransactionDto>> GetTransactionsAsync(TransactionFilterDto filter);
    }
} 