using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CashFlowTransactions.Domain.Entities;
using CashFlowTransactions.Domain.Interfaces;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CashFlowTransactions.Infra.Message.Kafka
{
    public class KafkaTransactionConsumer : ITransactionQueueConsumer
    {
        private readonly IConsumer<Ignore, string> _consumer;
        private readonly string _topic;

        public event EventHandler<Transaction> OnMessageReceived;

        public KafkaTransactionConsumer(IConfiguration config)
        {
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = config["Kafka:BootstrapServers"],
                GroupId = config["Kafka:GroupId"],
                AutoOffsetReset = Enum.Parse<AutoOffsetReset>(config["Kafka:AutoOffsetReset"]),
                EnableAutoCommit = true
            };

            _consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
            _topic = config["Kafka:Topic"];
        }

        public async Task ConsumeAsync(CancellationToken cancellationToken)
        {
            _consumer.Subscribe(_topic);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = _consumer.Consume(cancellationToken);
                        if (consumeResult?.Message?.Value != null)
                        {
                            
                            var transaction = JsonConvert.DeserializeObject<Transaction>(consumeResult.Message.Value);
                            if (transaction != null)
                            {
                                OnMessageReceived?.Invoke(this, transaction);
                            }
                        }
                    }
                    catch (ConsumeException ex)
                    {
                    }
                }
            }
            finally
            {
                _consumer.Close();
            }
        }
    }
} 