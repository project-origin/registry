using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Registry.Server.Interfaces;
using ProjectOrigin.Registry.V1;
using RabbitMQ.Client.Events;

namespace ProjectOrigin.Registry.Server.Services;

public class VerifyTransactionWorker : IDisposable
{
    private readonly ILogger<VerifyTransactionWorker> _logger;
    private readonly IRabbitMqChannel _channel;
    private readonly string _queueName;
    private readonly VerifyTransactionConsumer _transactionVerifier;
    private readonly string _consumerTag;
    private AsyncEventingBasicConsumer? _consumer;

    public VerifyTransactionWorker(ILogger<VerifyTransactionWorker> logger, IRabbitMqChannel channel, string queueName, VerifyTransactionConsumer transactionVerifier)
    {
        _logger = logger;
        _channel = channel;
        _queueName = queueName;
        _consumerTag = $"consumer.{queueName}";
        _transactionVerifier = transactionVerifier;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            _logger.LogInformation("Starting VerifyTransactionWorker for queue {queueName}", _queueName);

            _channel.Channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
                );

            _channel.Channel.BasicQos(
                    prefetchSize: 0,
                    prefetchCount: 5,
                    global: false
                    );

            _consumer = new AsyncEventingBasicConsumer(_channel.Channel);
            _consumer.Received += Consumer_Received;

            _channel.Channel.BasicConsume(
                queue: _queueName,
                autoAck: false,
                consumerTag: _consumerTag,
                noLocal: false,
                exclusive: true,
                arguments: null,
                consumer: _consumer
                );
        }, cancellationToken);
    }

    public void Dispose()
    {
        _channel.Channel.BasicCancel(_consumerTag);
        _channel.Dispose();
    }

    private async Task Consumer_Received(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var transaction = Transaction.Parser.ParseFrom(ea.Body.ToArray());
            var streamId = transaction.Header.FederatedStreamId.StreamId.ToString();

            await _transactionVerifier.Verify(transaction);

            _channel.Channel.BasicAck(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transaction");
            _channel.Channel.BasicNack(ea.DeliveryTag, false, true);
        }
    }
}
