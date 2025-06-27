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
        /// Obtém transações com paginação e filtros
        /// </summary>
        /// <param name="pageNumber">Número da página (padrão: 1)</param>
        /// <param name="pageSize">Tamanho da página (padrão: 10, máximo: 100)</param>
        /// <param name="startDate">Data inicial do filtro</param>
        /// <param name="endDate">Data final do filtro</param>
        /// <param name="type">Tipo da transação (Credit/Debit)</param>
        /// <param name="minAmount">Valor mínimo</param>
        /// <param name="maxAmount">Valor máximo</param>
        /// <param name="description">Filtro por descrição (busca parcial)</param>
        /// <param name="origin">Filtro por origem (busca parcial)</param>
        /// <returns>Lista paginada de transações</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponseDto<TransactionDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponseDto<TransactionDto>>), 404)]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponseDto<TransactionDto>>), 500)]
        public async Task<ActionResult<PaginatedResponseDto<TransactionDto>>> GetTransactions(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] Domain.Enums.TransactionType? type = null,
            [FromQuery] decimal? minAmount = null,
            [FromQuery] decimal? maxAmount = null,
            [FromQuery] string? description = null,
            [FromQuery] string? origin = null)
        {
            try 
            {
                _logger.LogInformation($"Controller: Iniciando GetTransactions com page={pageNumber}, size={pageSize}");
                
                // Limitar o tamanho máximo
                pageSize = pageSize > 100 ? 100 : pageSize;
                pageSize = pageSize <= 0 ? 10 : pageSize;
                pageNumber = pageNumber <= 0 ? 1 : pageNumber;
                
                var filter = new TransactionFilterDto
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    StartDate = startDate,
                    EndDate = endDate,
                    Type = type,
                    MinAmount = minAmount,
                    MaxAmount = maxAmount,
                    Description = description,
                    Origin = origin
                };
                
                var paginatedResult = await _transactionService.GetTransactionsAsync(filter);
                
                _logger.LogInformation($"Controller: GetTransactions recebeu {paginatedResult.Items?.Count() ?? 0} itens, " + 
                    $"TotalCount={paginatedResult.TotalCount}, TotalPages={paginatedResult.TotalPages}");
                
                var response = ApiResponse<PaginatedResponseDto<TransactionDto>>.Ok(
                    paginatedResult, 
                    "Transações obtidas com sucesso");
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter transações");
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