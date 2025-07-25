﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CashFlowTransactions.Application.DTOs;
using CashFlowTransactions.Domain.Entities;
using CashFlowTransactions.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CashFlowTransactions.Application.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionQueuePublisher _publisher;
        private readonly ITransactionRepository _repository;
        private readonly ILogger<TransactionService>? _logger;

        public TransactionService(
            ITransactionQueuePublisher publisher, 
            ITransactionRepository repository,
            ILogger<TransactionService>? logger = null)
        {
            _publisher = publisher;
            _repository = repository;
            _logger = logger;
        }

        public async Task<(Transaction transaction, string messageId)> RegisterAsync(Transaction transaction)
        {
            var messageId = await _publisher.PublishAsync(transaction);
            
            return (transaction, messageId);
        }

        public async Task<(Transaction transaction, string messageId)> RegisterAsync(CreateTransactionDto createDto)
        {
            // Criar a transação usando o construtor da entidade que já trata a conversão UTC
            var transaction = new Transaction(
                createDto.Amount,
                createDto.Type,
                createDto.TransactionDate,
                createDto.Description,
                createDto.Origin
            );

            var messageId = await _publisher.PublishAsync(transaction);
            
            return (transaction, messageId);
        }

        public async Task<IEnumerable<Transaction>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Transaction?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<PaginatedResponseDto<TransactionDto>> GetTransactionsAsync(TransactionFilterDto filter)
        {
            // Validar e normalizar valores de paginação
            var pageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber;
            var pageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;
            pageSize = pageSize > 100 ? 100 : pageSize; // Limitar máximo
            
            // Obter a queryable base
            var query = _repository.GetAll();

            // Aplicar filtros
            if (filter.StartDate.HasValue)
                query = query.Where(t => t.TransactionDate.Date >= filter.StartDate.Value.Date);

            if (filter.EndDate.HasValue)
                query = query.Where(t => t.TransactionDate.Date <= filter.EndDate.Value.Date);

            if (filter.Type.HasValue)
                query = query.Where(t => t.Type == filter.Type.Value);

            if (filter.MinAmount.HasValue)
                query = query.Where(t => t.Amount >= filter.MinAmount.Value);

            if (filter.MaxAmount.HasValue)
                query = query.Where(t => t.Amount <= filter.MaxAmount.Value);

            if (!string.IsNullOrWhiteSpace(filter.Description))
                query = query.Where(t => t.Description != null && t.Description.Contains(filter.Description));

            if (!string.IsNullOrWhiteSpace(filter.Origin))
                query = query.Where(t => t.Origin != null && t.Origin.Contains(filter.Origin));

            // Ordenar sempre da transação mais recente para a mais antiga (order by desc)
            query = query.OrderByDescending(t => t.TransactionDate)
                         .ThenByDescending(t => t.CreatedAt);

            // Contar o total antes de paginar
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Aplicar paginação usando valores normalizados
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Mapear manualmente para DTOs
            var transactionDtos = items.Select(t => new TransactionDto
            {
                Id = t.Id,
                Description = t.Description,
                Amount = t.Amount,
                Type = t.Type,
                Origin = t.Origin,
                TransactionDate = t.TransactionDate,
                CreatedAt = t.CreatedAt
            }).ToList();

            // Retornar resultado paginado com valores normalizados
            return new PaginatedResponseDto<TransactionDto>(
                transactionDtos,
                pageNumber,
                pageSize,
                totalCount,
                totalPages);
        }
    }
}
