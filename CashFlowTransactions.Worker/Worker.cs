using CashFlowTransactions.Domain.Entities;
using CashFlowTransactions.Domain.Interfaces;

namespace CashFlowTransactions.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ITransactionQueueConsumer _consumer;

        public Worker(ITransactionQueueConsumer consumer)
        {
            _consumer = consumer;
            _consumer.OnMessageReceived += HandleTransactionReceived;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Iniciar o consumer
            await _consumer.ConsumeAsync(stoppingToken);
        }

        private void HandleTransactionReceived(object sender, Transaction transaction)
        {
            // Aqui podemos adicionar lógica adicional de processamento se necessário
            // O consumer já está persistindo a transação no banco de dados
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            await base.StopAsync(stoppingToken);
        }
    }
}
