using Microsoft.AspNetCore.Mvc;
using CashFlowTransactions.Application.Services;
using CashFlowTransactions.Domain.Entities;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using CashFlowTransactions.Application.DTOs;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Net;
using CashFlowTransactions.API.Models;

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
        /// <returns>Resultado da operação com o ID da mensagem no Kafka</returns>
        [HttpPost]
        public async Task<IActionResult> RegisterTransaction([FromBody] CreateTransactionDto createDto)
        {
            if (createDto == null)
                return BadRequest("Transação inválida");

            // Usar o serviço que já faz a conversão correta
            var (savedTransaction, messageId) = await _transactionService.RegisterAsync(createDto);
            
            // Retornar apenas o UUID da mensagem do Kafka
            var result = new 
            { 
                MessageId = messageId
            };
            
            var response = ApiResponse<object>.Ok(result, $"Transação registrada com sucesso");
            return CreatedAtAction(nameof(GetTransactionById), new { id = savedTransaction.Id }, response);
        }

        /// <summary>
        /// Obtém todas as transações
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<TransactionDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<TransactionDto>>), 404)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<TransactionDto>>), 500)]

        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetAllTransactions()
        {
            try 
            {
                _logger.LogInformation("Controller: Iniciando GetAllTransactions");
                
                var transactions = await _transactionService.GetAllAsync();
                
                _logger.LogInformation($"Controller: GetAllTransactions recebeu {(transactions?.Count() ?? 0)} transações do serviço");
                
                if (transactions == null || !transactions.Any())
                {
                    _logger.LogInformation("Controller: Nenhuma transação encontrada");
                    var emptyResponse = ApiResponse<IEnumerable<TransactionDto>>.Ok(
                        Enumerable.Empty<TransactionDto>(), 
                        "Nenhuma transação encontrada");
                    return Ok(emptyResponse);
                }
                
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
                
                var response = ApiResponse<IEnumerable<TransactionDto>>.Ok(transactionDtos, $"Transações obtidas com sucesso");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter todas as transações");
                var errorResponse = ApiResponse<IEnumerable<TransactionDto>>.Error("Erro interno do servidor");
                return StatusCode(500, errorResponse);
            }
        }


        /// <summary>
        /// Obtém transações com paginação simples
        /// </summary>
        [HttpGet("paginated")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponseDto<TransactionDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponseDto<TransactionDto>>), 404)]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponseDto<TransactionDto>>), 500)]

        public async Task<ActionResult<PaginatedResponseDto<TransactionDto>>> GetPaginatedTransactions(
            [FromQuery] int page = 1, 
            [FromQuery] int size = 10)
        {
            try
            {
                _logger.LogInformation($"Controller: Iniciando GetPaginatedTransactions com page={page}, size={size}");
                
                // Limitar o tamanho máximo a 10
                size = size > 10 ? 10 : size;
                _logger.LogInformation($"Controller: Tamanho ajustado para size={size}");
                
                var (Items, TotalCount, TotalPages) = await _transactionService.GetPaginatedTransactionsAsync(page, size);
                
                _logger.LogInformation($"Controller: GetPaginatedTransactions recebeu {Items?.Count() ?? 0} itens, " + 
                    $"TotalCount={TotalCount}, TotalPages={TotalPages}");
                
                if (Items == null || !Items.Any())
                {
                    _logger.LogInformation("Controller: Nenhuma transação paginada encontrada");
                    
                    // Criar resposta vazia mas bem formatada
                    var emptyPagination = new PaginatedResponseDto<TransactionDto>(
                        items: Enumerable.Empty<TransactionDto>(),
                        pageNumber: page,
                        pageSize: size,
                        totalCount: 0,
                        totalPages: 0
                    );
                    
                    var emptyResponse = ApiResponse<PaginatedResponseDto<TransactionDto>>.Ok(
                        emptyPagination, 
                        "Nenhuma transação encontrada");
                    
                    return Ok(emptyResponse);
                }
                
                var paginatedResult = new PaginatedResponseDto<TransactionDto>(
                    items: Items,
                    pageNumber: page,
                    pageSize: size,
                    totalCount: TotalCount,
                    totalPages: TotalPages
                );
                
                var response = ApiResponse<PaginatedResponseDto<TransactionDto>>.Ok(paginatedResult, $"Transações paginadas obtidas com sucesso");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter transações paginadas");
                var errorResponse = ApiResponse<PaginatedResponseDto<TransactionDto>>.Error("Erro interno do servidor");
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Obtém uma transação pelo ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<TransactionDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<TransactionDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<TransactionDto>), 500)]

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
            var response = ApiResponse<TransactionDto>.Ok(transactionDto, $"Transação obtida com sucesso");
            return Ok(response);
        }
    }
} 