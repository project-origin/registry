using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Registry.Extensions;
using ProjectOrigin.Registry.MessageBroker;
using ProjectOrigin.Registry.V1;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ProjectOrigin.Registry.TransactionProcessor;

public sealed class TransactionProcessorWorker : IAsyncDisposable
{
    private readonly ILogger<TransactionProcessorWorker> _logger;
    private readonly IRabbitMqChannel _channel;
    private readonly string _queueName;
    private readonly TransactionProcessorDispatcher _transactionVerifier;
    private readonly IQueueResolver _queueResolver;
    private readonly string _consumerTag;

    public TransactionProcessorWorker(
        ILogger<TransactionProcessorWorker> logger,
        IRabbitMqChannel channel,
        string queueName,
        TransactionProcessorDispatcher transactionVerifier,
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
                arguments: null,
                cancellationToken: cancellationToken);

            await _channel.Channel.BasicQosAsync(
                    prefetchSize: 0,
                    prefetchCount: 5,
                    global: false,
                    cancellationToken: cancellationToken);

            var consumer = new AsyncEventingBasicConsumer(_channel.Channel);
            consumer.ReceivedAsync += Consumer_Received;

            await _channel.Channel.BasicConsumeAsync(
                queue: _queueName,
                autoAck: false,
                consumerTag: _consumerTag,
                noLocal: false,
                exclusive: true,
                arguments: null,
                consumer: consumer,
                cancellationToken: cancellationToken);

        }, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.Channel.BasicCancelAsync(_consumerTag);
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

            await _channel.Channel.BasicAckAsync(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transaction");
            await _channel.Channel.BasicNackAsync(ea.DeliveryTag, false, true);
        }
    }
}
