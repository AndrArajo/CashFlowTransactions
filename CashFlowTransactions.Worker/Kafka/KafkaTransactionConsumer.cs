using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CashFlowTransactions.Domain.Interfaces;
using Confluent.Kafka;

namespace CashFlowTransactions.Worker.Kafka
{
    class KafkaTransactionConsumer
    {
        private readonly IConfiguration _configuration;

        public KafkaTransactionConsumer(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        //public Task StartConsumingAsync(CancellationToken stoppingToken)
        //{
        //    var broker = _configuration["Kafka:BootstrapServers"];
        //    var topic = _configuration["Kafka:Topic"];

        //    // Consumo aqui...
        //}
    }
}
