using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CashFlowTransactions.Domain.Entities;
using CashFlowTransactions.Domain.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace CashFlowTransactions.Infra.Message.Rabbitmq
{
    public class RabbitmqTransactionConsumer : BackgroundService, ITransactionQueueConsumer
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _queueName;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitmqTransactionConsumer>? _logger;

        public event EventHandler<Transaction>? OnMessageReceived;

        public RabbitmqTransactionConsumer(IConfiguration config, IServiceScopeFactory serviceScopeFactory, ILogger<RabbitmqTransactionConsumer>? logger = null)
        {
            _configuration = config ?? throw new ArgumentNullException(nameof(config));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _logger = logger;

            var factory = new ConnectionFactory()
            {
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? config["RabbitMQ:HostName"] ?? "localhost",
                UserName = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? config["RabbitMQ:UserName"] ?? "guest",
                Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? config["RabbitMQ:Password"] ?? "guest",
                VirtualHost = Environment.GetEnvironmentVariable("RABBITMQ_VHOST") ?? config["RabbitMQ:VirtualHost"] ?? "/",
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _queueName = Environment.GetEnvironmentVariable("RABBITMQ_QUEUE_NAME") ?? config["RabbitMQ:QueueName"] ?? "transactions";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await ConsumeAsync(stoppingToken);
        }

        public async Task ConsumeAsync(CancellationToken cancellationToken)
        {
            _channel.QueueDeclare(queue: _queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = System.Text.Encoding.UTF8.GetString(body);
                    var transaction = JsonConvert.DeserializeObject<Transaction>(message);
                    
                    if (transaction != null)
                    {
                        try
                        {
                            using (var scope = _serviceScopeFactory.CreateScope())
                            {
                                var repository = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
                                await repository.AddAsync(transaction);
                            }
                            
                            OnMessageReceived?.Invoke(this, transaction);
                            
                            // Acknowledge da mensagem após processamento bem-sucedido
                            _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Erro ao processar transação: {Message}", ex.Message);
                            // Reject da mensagem em caso de erro
                            _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                        }
                    }
                    else
                    {
                        _logger?.LogWarning("Mensagem recebida não pôde ser deserializada como Transaction: {Message}", message);
                        _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Erro ao consumir mensagem do RabbitMQ: {Message}", ex.Message);
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(queue: _queueName,
                                autoAck: false, // Desabilitar auto-ack para controlar manualmente
                                consumer: consumer);

            // Manter o consumer ativo enquanto não for cancelado
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100, cancellationToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _channel?.Close();
            _connection?.Close();
            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
