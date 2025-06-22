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
using Microsoft.Extensions.Logging;

namespace CashFlowTransactions.Infra.Message.Kafka
{
    public class KafkaTransactionConsumer : BackgroundService, ITransactionQueueConsumer
    {
        private readonly IConsumer<Ignore, string> _consumer;
        private readonly string _topic;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<KafkaTransactionConsumer>? _logger;

        public event EventHandler<Transaction>? OnMessageReceived;

        public KafkaTransactionConsumer(IConfiguration config, IServiceScopeFactory serviceScopeFactory, ILogger<KafkaTransactionConsumer>? logger = null)
        {
            _configuration = config ?? throw new ArgumentNullException(nameof(config));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _logger = logger;
            
            var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? config["Kafka:BootstrapServers"] ?? "localhost:9092";
            var groupId = Environment.GetEnvironmentVariable("KAFKA_GROUP_ID") ?? config["Kafka:GroupId"] ?? "transaction-consumer-group";
            var autoOffsetResetValue = Environment.GetEnvironmentVariable("KAFKA_AUTO_OFFSET_RESET") ?? config["Kafka:AutoOffsetReset"];
            
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = ParseAutoOffsetReset(autoOffsetResetValue),
                EnableAutoCommit = true
            };

            _consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
            _topic = Environment.GetEnvironmentVariable("KAFKA_TOPIC") ?? config["Kafka:Topic"] ?? "transactions";
            
            if (string.IsNullOrEmpty(_topic))
            {
                throw new ArgumentException("Kafka:Topic configuration is missing or empty");
            }
        }

        private AutoOffsetReset ParseAutoOffsetReset(string? value)
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
                    catch (ConsumeException ex)
                    {
                        // Registrar o erro usando logger
                        _logger?.LogError(ex, "Erro ao consumir mensagem do Kafka: {Message}", ex.Message);
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