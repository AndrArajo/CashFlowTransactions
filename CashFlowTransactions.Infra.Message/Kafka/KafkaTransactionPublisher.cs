using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CashFlowTransactions.Domain.Entities;
using CashFlowTransactions.Domain.Interfaces;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CashFlowTransactions.Infra.Message.Kafka
{
    public class KafkaTransactionPublisher : ITransactionQueuePublisher
    {
        private readonly IProducer<Null, string> _producer;
        private readonly string _topic;

        public KafkaTransactionPublisher(IConfiguration config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            
            var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? config["Kafka:BootstrapServers"] ?? "localhost:9092";
            var configKafka = new ProducerConfig
            {
                BootstrapServers = bootstrapServers
            };

            _producer = new ProducerBuilder<Null, string>(configKafka).Build();
            _topic = Environment.GetEnvironmentVariable("KAFKA_TOPIC") ?? config["Kafka:Topic"] ?? "transactions";
            
            if (string.IsNullOrEmpty(_topic))
            {
                throw new ArgumentException("Kafka:Topic configuration is missing or empty");
            }
        }

        public async Task<string> PublishAsync(Transaction transaction)
        {
            try
            {
                string messageId = Guid.NewGuid().ToString();
                
                transaction.MessageId = messageId;
                
                var message = JsonConvert.SerializeObject(transaction);
                
                _producer.Produce(_topic, new Message<Null, string> { Value = message });
                
                return messageId;
            }
            catch (Exception ex)
            {
                // Registrar o erro e relançar
                throw new Exception($"Error publishing message to Kafka: {ex.Message}", ex);
            }
        }
    }
}
