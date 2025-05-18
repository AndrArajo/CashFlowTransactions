using Microsoft.AspNetCore.Mvc;
using CashFlowTransactions.Application.Services;
using CashFlowTransactions.Domain.Entities;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using CashFlowTransactions.Application.DTOs;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace CashFlowTransactions.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly ILogger<TransactionController> _logger;

        public TransactionController(ITransactionService transactionService, ILogger<TransactionController> logger)
        {
            _transactionService = transactionService;
            _logger = logger;
        }

        /// <summary>
        /// Registra uma nova transação financeira e a envia para o Kafka
        /// </summary>
        /// <param name="createDto">Dados da transação</param>
        /// <returns>Resultado da operação</returns>
        [HttpPost]
        public async Task<IActionResult> RegisterTransaction([FromBody] CreateTransactionDto createDto)
        {
            if (createDto == null)
                return BadRequest("Transação inválida");

            // Usar o serviço que já faz a conversão correta
            var savedTransaction = await _transactionService.RegisterAsync(createDto);
            
            // Criar DTO manualmente para retorno
            var transactionDto = new TransactionDto
            {
                Id = savedTransaction.Id,
                Description = savedTransaction.Description,
                Amount = savedTransaction.Amount,
                Type = savedTransaction.Type,
                Origin = savedTransaction.Origin,
                TransactionDate = savedTransaction.TransactionDate,
                CreatedAt = savedTransaction.CreatedAt
            };
            
            return CreatedAtAction(nameof(GetTransactionById), new { id = savedTransaction.Id }, transactionDto);
        }

        /// <summary>
        /// Obtém todas as transações
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetAllTransactions()
        {
            var transactions = await _transactionService.GetAllAsync();
            
            var transactionDtos = transactions.Select(t => new TransactionDto
            {
                Id = t.Id,
                Description = t.Description,
                Amount = t.Amount,
                Type = t.Type,
                Origin = t.Origin,
                TransactionDate = t.TransactionDate,
                CreatedAt = t.CreatedAt
            });
            
            return Ok(transactionDtos);
        }

        /// <summary>
        /// Obtém transações com paginação e filtros
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<PaginatedResponseDto<TransactionDto>>> GetPagedTransactions([FromQuery] TransactionFilterDto filter)
        {
            var transactions = await _transactionService.GetTransactionsAsync(filter);
            return Ok(transactions);
        }

        /// <summary>
        /// Obtém transações com paginação simples
        /// </summary>
        [HttpGet("paginated")]
        public async Task<ActionResult<PaginatedResponseDto<TransactionDto>>> GetPaginatedTransactions(
            [FromQuery] int page = 1, 
            [FromQuery] int size = 10)
        {
            try
            {
                // Limitar o tamanho máximo a 10
                size = size > 10 ? 10 : size;
                
                var (Items, TotalCount, TotalPages) = await _transactionService.GetPaginatedTransactionsAsync(page, size);
                
                var response = new PaginatedResponseDto<TransactionDto>(
                    items: Items,
                    pageNumber: page,
                    pageSize: size,
                    totalCount: TotalCount,
                    totalPages: TotalPages
                );
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar transações paginadas");
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        /// <summary>
        /// Obtém uma transação pelo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionDto>> GetTransactionById(int id)
        {
            var transaction = await _transactionService.GetByIdAsync(id);
            
            if (transaction == null)
                return NotFound();
            
            var transactionDto = new TransactionDto
            {
                Id = transaction.Id,
                Description = transaction.Description,
                Amount = transaction.Amount,
                Type = transaction.Type,
                Origin = transaction.Origin,
                TransactionDate = transaction.TransactionDate,
                CreatedAt = transaction.CreatedAt
            };
            
            return Ok(transactionDto);
        }
    }
} 