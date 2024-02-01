using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Registry.Server.Extensions;
using ProjectOrigin.Registry.Server.Interfaces;
using ProjectOrigin.Registry.V1;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ProjectOrigin.Registry.Server.Services;

public sealed class TransactionProcessorWorker : IDisposable
{
    private readonly ILogger<TransactionProcessorWorker> _logger;
    private readonly IRabbitMqChannel _channel;
    private readonly string _queueName;
    private readonly TransactionProcessor _transactionVerifier;
    private readonly IQueueResolver _queueResolver;
    private readonly string _consumerTag;

    public TransactionProcessorWorker(
        ILogger<TransactionProcessorWorker> logger,
        IRabbitMqChannel channel,
        string queueName,
        TransactionProcessor transactionVerifier,
        IQueueResolver queueResolver)
    {
        _logger = logger;
        _channel = channel;
        _queueName = queueName;
        _consumerTag = $"consumer.{queueName}";
        _transactionVerifier = transactionVerifier;
        _queueResolver = queueResolver;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            _logger.LogInformation("Starting VerifyTransactionWorker for queue {queueName}", _queueName);

            await _channel.Channel.QueueDeclareAsync(
                queue: _queueName,
                passive: false,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
                );

            await _channel.Channel.BasicQosAsync(
                    prefetchSize: 0,
                    prefetchCount: 5,
                    global: false
                    );

            var consumer = new AsyncEventingBasicConsumer(_channel.Channel);
            consumer.Received += Consumer_Received;

            await _channel.Channel.BasicConsumeAsync(
                queue: _queueName,
                autoAck: false,
                consumerTag: _consumerTag,
                noLocal: false,
                exclusive: true,
                arguments: null,
                consumer: consumer
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
            var targetQueue = _queueResolver.GetQueueName(transaction);

            if (targetQueue == _queueName)
            {
                await _transactionVerifier.Verify(transaction);
            }
            else
            {
                _logger.LogWarning("Received transaction for wrong queue {current_queue}, requeing to {new_queue}", _queueName, targetQueue);
                await _channel.Channel.BasicPublishAsync("", targetQueue, ea.Body);
            }

            _channel.Channel.BasicAck(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transaction");
            _channel.Channel.BasicNack(ea.DeliveryTag, false, true);
        }
    }
}
