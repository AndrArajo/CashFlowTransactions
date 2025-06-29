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
using Microsoft.Extensions.Logging;

namespace CashFlowTransactions.Infra.Data.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);
        private readonly ILogger<TransactionRepository>? _logger;

        public TransactionRepository(ApplicationDbContext context, ICacheService cacheService, ILogger<TransactionRepository>? logger = null)
        {
            _context = context;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Transaction> AddAsync(Transaction transaction)
        {
            await _context.Transactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
            
            // Invalidar completamente o cache após adicionar uma nova transação
            await _cacheService.RemoveAsync("transactions_all");
            await _cacheService.RemoveAsync("transactions_page_1_size_10");
            
            // Tentar invalidar outras chaves comuns
            for (int size = 5; size <= 20; size += 5)
            {
                await _cacheService.RemoveAsync($"transactions_page_1_size_{size}");
            }

            return transaction;
        }


        public async Task<IEnumerable<Transaction>> GetAllAsync()
        {
            _logger?.LogInformation("Buscando todas as transações, possivelmente do cache");
            
            // Verificar a contagem direta do banco antes de usar o cache
            var dbCount = await _context.Transactions.CountAsync();
            _logger?.LogInformation($"Total de transações no banco (GetAllAsync): {dbCount}");
            
            // Se não há transações no banco, retornar lista vazia sem usar cache
            if (dbCount == 0)
            {
                _logger?.LogInformation("Nenhuma transação no banco, retornando lista vazia");
                return Enumerable.Empty<Transaction>();
            }
            
            var result = await _cacheService.GetOrCreateAsync(
                "transactions_all",
                async () => 
                {
                    _logger?.LogInformation("Cache miss para todas as transações, buscando do banco de dados");
                    return await _context.Transactions.ToListAsync();
                },
                _cacheExpiration);
                
            // Verificar se o cache retornou algo inconsistente (vazio quando deveria ter dados)
            if ((result == null || !result.Any()) && dbCount > 0)
            {
                _logger?.LogWarning("Cache inconsistente! Retornou vazio quando há dados no banco. Forçando atualização.");
                // Forçar nova leitura do banco
                await _cacheService.RemoveAsync("transactions_all");
                return await _context.Transactions.ToListAsync();
            }
            
            return result ?? Enumerable.Empty<Transaction>();
        }

        public IQueryable<Transaction> GetAll()
        {
            // Para IQueryable, sempre retornamos do Entity Framework para manter compatibilidade
            // com operações assíncronas (CountAsync, ToListAsync, etc.)
            // O cache será aplicado quando necessário nos métodos específicos
            _logger?.LogInformation("GetAll: Retornando IQueryable do Entity Framework");
            return _context.Transactions.AsQueryable();
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
            _logger?.LogInformation($"Buscando transações paginadas: página {pageNumber}, tamanho {pageSize}");
            
            // Verificar diretamente o banco de dados antes de tentar o cache
            var dbCount = await _context.Transactions.CountAsync();
            _logger?.LogInformation($"Total de transações no banco (GetPaginatedAsync): {dbCount}");
            
            // Se não há transações no banco, retornar resultado vazio sem usar cache
            if (dbCount == 0)
            {
                _logger?.LogInformation("Nenhuma transação no banco, retornando paginação vazia");
                return (Enumerable.Empty<Transaction>(), 0);
            }
            
            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            string cacheKey = $"transactions_page_{pageNumber}_size_{pageSize}";
            _logger?.LogInformation($"Usando chave de cache: {cacheKey}");

            // Simplificando a lógica para sempre usar o cache ou criar se não existir
            var result = await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () => 
                {
                    _logger?.LogInformation("Cache miss para transações paginadas, buscando do banco de dados");
                    
                    var totalCount = await _context.Transactions.CountAsync();
                    _logger?.LogInformation($"Total de transações no banco: {totalCount}");

                    if (totalCount == 0)
                    {
                        _logger?.LogInformation("Nenhuma transação encontrada no banco de dados");
                        return (Enumerable.Empty<Transaction>(), 0);
                    }

                    var items = await _context.Transactions
                        .OrderByDescending(t => t.Id)
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();

                    _logger?.LogInformation($"Transações encontradas: {items.Count}");
                    return (items, totalCount);
                },
                _cacheExpiration);
                
            // Garantir que items nunca seja nulo
            var resultItems = result.Item1 ?? Enumerable.Empty<Transaction>();
            var resultCount = result.Item2;
            
            // Verificar existência da chave após a criação
            var keyExists = await _cacheService.KeyExistsAsync(cacheKey);
            _logger?.LogInformation($"A chave '{cacheKey}' existe no cache: {keyExists}");
            
            // Verificar inconsistência entre o cache e o banco
            if (!resultItems.Any() && dbCount > 0)
            {
                _logger?.LogWarning("Cache inconsistente! Retornou itens vazios quando há dados no banco. Forçando consulta direta.");
                await _cacheService.RemoveAsync(cacheKey);
                var directResult = await GetPaginatedDirectFromDatabaseAsync(pageNumber, pageSize);
                
                // Armazenar resultado direto no cache
                await _cacheService.SetAsync(cacheKey, directResult, _cacheExpiration);
                
                return directResult;
            }
            
            _logger?.LogInformation($"Retornando {(resultItems as ICollection<Transaction>)?.Count ?? 0} transações");
            
            return (resultItems, resultCount);
        }
        
        public async Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPaginatedDirectFromDatabaseAsync(int pageNumber, int pageSize)
        {
            _logger?.LogInformation($"Buscando transações paginadas DIRETAMENTE DO BANCO: página {pageNumber}, tamanho {pageSize}");
            
            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            try 
            {
                var totalCount = await _context.Transactions.CountAsync();
                _logger?.LogInformation($"GetPaginatedDirectFromDatabaseAsync: Total de transações no banco: {totalCount}");

                if (totalCount == 0)
                {
                    _logger?.LogInformation("GetPaginatedDirectFromDatabaseAsync: Nenhuma transação encontrada no banco de dados");
                    return (Enumerable.Empty<Transaction>(), 0);
                }

                var items = await _context.Transactions
                    .OrderByDescending(t => t.TransactionDate)
                    .ThenByDescending(t => t.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                _logger?.LogInformation($"GetPaginatedDirectFromDatabaseAsync: Transações encontradas: {items.Count}");
                return (items, totalCount);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "GetPaginatedDirectFromDatabaseAsync: Erro ao buscar transações do banco de dados");
                throw;
            }
        }
    }
} 