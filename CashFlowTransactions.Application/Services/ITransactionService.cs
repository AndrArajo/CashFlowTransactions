using System.Collections.Generic;
using System.Threading.Tasks;
using CashFlowTransactions.Application.DTOs;
using CashFlowTransactions.Domain.Entities;

namespace CashFlowTransactions.Application.Services
{
    public interface ITransactionService
    {
        Task<Transaction> RegisterAsync(Transaction transaction);
        Task<Transaction> RegisterAsync(CreateTransactionDto createDto);
        Task<IEnumerable<Transaction>> GetAllAsync();
        Task<Transaction?> GetByIdAsync(int id);
        Task<PaginatedResponseDto<TransactionDto>> GetTransactionsAsync(TransactionFilterDto filter);
        Task<(IEnumerable<TransactionDto> Items, int TotalCount, int TotalPages)> GetPaginatedTransactionsAsync(int pageNumber, int pageSize);
    }
} 