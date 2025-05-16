using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CashFlowTransactions.Domain.Entities;
using CashFlowTransactions.Domain.Interfaces;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace CashFlowTransactions.Infra.Message.Kafka
{
    public class KafkaTransactionConsumer : BackgroundService, ITransactionQueueConsumer
    {
        private readonly IConsumer<Ignore, string> _consumer;
        private readonly string _topic;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConfiguration _configuration;

        public event EventHandler<Transaction> OnMessageReceived;

        public KafkaTransactionConsumer(IConfiguration config, IServiceScopeFactory serviceScopeFactory)
        {
            _configuration = config ?? throw new ArgumentNullException(nameof(config));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = config["Kafka:BootstrapServers"],
                GroupId = config["Kafka:GroupId"],
                AutoOffsetReset = ParseAutoOffsetReset(config["Kafka:AutoOffsetReset"]),
                EnableAutoCommit = true
            };

            _consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
            _topic = config["Kafka:Topic"];
        }

        private AutoOffsetReset ParseAutoOffsetReset(string value)
        {
            // Tratar o valor considerando case insensitive
            return value?.ToLower() switch
            {
                "earliest" => AutoOffsetReset.Earliest,
                "latest" => AutoOffsetReset.Latest,
                "error" => AutoOffsetReset.Error,
                _ => AutoOffsetReset.Earliest // valor padrão
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await ConsumeAsync(stoppingToken);
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
                                using (var scope = _serviceScopeFactory.CreateScope())
                                {
                                    var repository = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
                                    await repository.AddAsync(transaction);
                                }
                                
                                OnMessageReceived?.Invoke(this, transaction);
                            }
                        }
                    }
                    catch (ConsumeException)
                    {
                        await Task.Delay(1000, cancellationToken);
                    }
                }
            }
            finally
            {
                _consumer.Close();
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _consumer.Close();
            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _consumer?.Dispose();
            base.Dispose();
        }
    }
} 