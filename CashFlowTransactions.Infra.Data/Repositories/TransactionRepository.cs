using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CashFlowTransactions.Domain.Entities;
using CashFlowTransactions.Domain.Enums;
using CashFlowTransactions.Domain.Interfaces;
using CashFlowTransactions.Infra.Data.Context;
using CashFlowTransactions.Infra.CrossCutting.Caching;
using Microsoft.EntityFrameworkCore;

namespace CashFlowTransactions.Infra.Data.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

        public TransactionRepository(ApplicationDbContext context, ICacheService cacheService)
        {
            _context = context;
            _cacheService = cacheService;
        }

        public async Task<Transaction> AddAsync(Transaction transaction)
        {
            await _context.Transactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
            
            // Invalidar o cache após adicionar uma nova transação
            await _cacheService.RemoveAsync("transactions_all");
            return transaction;
        }


        public async Task<IEnumerable<Transaction>> GetAllAsync()
        {
            return await _cacheService.GetOrCreateAsync(
                "transactions_all",
                async () => await _context.Transactions.ToListAsync(),
                _cacheExpiration);
        }

        public IQueryable<Transaction> GetAll()
        {
            // Para IQueryable, não podemos cache diretamente, mas podemos armazenar 
            // os resultados após a materialização quando aplicado
            var cachedTransactions = _cacheService.GetOrCreateAsync(
                "transactions_all",
                async () => await _context.Transactions.ToListAsync(),
                _cacheExpiration).GetAwaiter().GetResult();
                
            return cachedTransactions.AsQueryable();
        }

        public async Task<Transaction?> GetByIdAsync(int id)
        {
            return await _cacheService.GetOrCreateAsync(
                $"transaction_{id}",
                async () => await _context.Transactions.FindAsync(id),
                _cacheExpiration);
        }

        public async Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPaginatedAsync(int pageNumber, int pageSize)
        {
            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            string cacheKey = $"transactions_page_{pageNumber}_size_{pageSize}";

            return await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () => 
                {
                    var totalCount = await _context.Transactions.CountAsync();

                    var items = await _context.Transactions
                        .OrderByDescending(t => t.TransactionDate)
                        .ThenByDescending(t => t.CreatedAt)
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();

                    return (items, totalCount);
                },
                _cacheExpiration);
        }
    }
} 