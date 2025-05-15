using Microsoft.AspNetCore.Mvc;
using CashFlowTransactions.Application.Services;
using CashFlowTransactions.Domain.Entities;
using System.Threading.Tasks;

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

            await _transactionService.RegisterAsync(transaction);
            
            return Ok("Transaction registered successfully");
        }
    }
} 