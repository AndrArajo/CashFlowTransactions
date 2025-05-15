using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CashFlowTransactions.Domain.Entities;
using CashFlowTransactions.Domain.Interfaces;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CashFlowTransactions.Infra.Message.Kafka
{
    public class KafkaTransactionPublisher : ITransactionQueuePublisher
    {
        private readonly IProducer<Null, string> _producer;
        private readonly string _topic;
        private readonly ILogger<KafkaTransactionPublisher> _logger;

        public KafkaTransactionPublisher(IConfiguration config, ILogger<KafkaTransactionPublisher> logger)
        {
            _logger = logger;
            var configKafka = new ProducerConfig
            {
                BootstrapServers = config["Kafka:BootstrapServers"]
            };

            _producer = new ProducerBuilder<Null, string>(configKafka).Build();
            _topic = config["Kafka:Topic"];
        }

        public async Task PublishAsync(Transaction transaction)
        {
            try
            {
                var message = JsonConvert.SerializeObject(transaction);
                _logger.LogInformation($"Publicando transação no Kafka: {message}");
                await _producer.ProduceAsync(_topic, new Message<Null, string> { Value = message });
                _logger.LogInformation($"Transação publicada com sucesso no tópico {_topic}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao publicar transação no Kafka");
                throw;
            }
        }
    }
}
