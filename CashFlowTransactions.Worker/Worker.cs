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
            // O consumer já é um background service e possui sua própria implementação
            // de ExecuteAsync, então não precisamos chamá-lo diretamente
            await Task.CompletedTask;
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
