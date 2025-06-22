using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CashFlowTransactions.Domain.Entities;
using CashFlowTransactions.Domain.Interfaces;
using RabbitMQ.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace CashFlowTransactions.Infra.Message.Rabbitmq
{
    public class RabbitmqTransactionPublisher : ITransactionQueuePublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _queueName;
        private readonly ILogger<RabbitmqTransactionPublisher>? _logger;

        public RabbitmqTransactionPublisher(IConfiguration config, ILogger<RabbitmqTransactionPublisher>? logger = null)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
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
            
            if (string.IsNullOrEmpty(_queueName))
            {
                throw new ArgumentException("RabbitMQ:QueueName configuration is missing or empty");
            }

            // Declarar a fila para garantir que existe
            _channel.QueueDeclare(queue: _queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
        }

        public async Task<string> PublishAsync(Transaction transaction)
        {
            try
            {
                string messageId = Guid.NewGuid().ToString();
                transaction.MessageId = messageId;

                var message = JsonConvert.SerializeObject(transaction);
                var body = Encoding.UTF8.GetBytes(message);

                _channel.BasicPublish(exchange: "", routingKey: _queueName, basicProperties: null, body: body);

                return messageId;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao publicar mensagem no RabbitMQ: {Message}", ex.Message);
                throw new Exception($"Error publishing message to RabbitMQ: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}
