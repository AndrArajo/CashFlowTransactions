using Microsoft.AspNetCore.Mvc;
using CashFlowTransactions.Application.Services;
using CashFlowTransactions.Domain.Entities;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace CashFlowTransactions.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly TransactionService _transactionService;

        public TransactionController(TransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        /// <summary>
        /// Registra uma nova transação financeira e a envia para o Kafka
        /// </summary>
        /// <param name="transaction">Dados da transação</param>
        /// <returns>Resultado da operação</returns>
        [HttpPost]
        public async Task<IActionResult> RegisterTransaction([FromBody] Transaction transaction)
        {
            if (transaction == null)
                return BadRequest("Transação inválida");

            var savedTransaction = await _transactionService.RegisterAsync(transaction);
            
            return CreatedAtAction(nameof(GetTransactionById), new { id = savedTransaction.Id }, savedTransaction);
        }

        /// <summary>
        /// Obtém todas as transações
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetAllTransactions()
        {
            var transactions = await _transactionService.GetAllAsync();
            return Ok(transactions);
        }

        /// <summary>
        /// Obtém uma transação pelo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Transaction>> GetTransactionById(int id)
        {
            var transaction = await _transactionService.GetByIdAsync(id);
            
            if (transaction == null)
                return NotFound();
                
            return Ok(transaction);
        }

       
    }
} 