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
                await _producer.ProduceAsync(_topic, new Message<Null, string> { Value = message });
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
