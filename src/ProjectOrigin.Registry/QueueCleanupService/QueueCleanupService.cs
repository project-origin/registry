using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Registry.Extensions;
using ProjectOrigin.Registry.MessageBroker;
using RabbitMQ.Client;

namespace ProjectOrigin.Registry;

public sealed class QueueCleanupService : BackgroundService, IDisposable
{
    private readonly ILogger<QueueCleanupService> _logger;
    private readonly IRabbitMqHttpClient _rabbitMqHttpClient;
    private readonly IRabbitMqChannelPool _rabbitMqChannelPool;
    private readonly IQueueResolver _queueResolver;
    private IRabbitMqChannel? _rabbitMqChannel;

    public QueueCleanupService(
        ILogger<QueueCleanupService> logger,
        IRabbitMqHttpClient rabbitMqHttpClient,
        IRabbitMqChannelPool rabbitMqChannelPool,
        IQueueResolver queueResolver)
    {
        _logger = logger;
        _rabbitMqHttpClient = rabbitMqHttpClient;
        _rabbitMqChannelPool = rabbitMqChannelPool;
        _queueResolver = queueResolver;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _rabbitMqChannel = await _rabbitMqChannelPool.GetChannelAsync();
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        do
        {
            var allQueues = await _rabbitMqHttpClient.GetQueuesAsync();
            var inactiveQueues = _queueResolver.GetInactiveQueues(allQueues.Select(x => x.Name));

            foreach (var inactiveQueue in allQueues.Where(x => inactiveQueues.Contains(x.Name)))
            {
                await FlushAndRemoveQueue(inactiveQueue);
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
        while (!stoppingToken.IsCancellationRequested);
    }

    private async Task FlushAndRemoveQueue(RabbitMqQueue queue)
    {
        _logger.LogInformation("Removing unused queue: {queue}", queue.Name);

        if (queue.Messages > 0)
        {
            _logger.LogInformation("Flushing queue: {queue} with {messageCount} messages", queue.Name, queue.Messages);

            await FlushAndSortQueue(queue.Name);
        }

        _logger.LogInformation("Deleting queue: {queue}", queue.Name);
        await _rabbitMqChannel!.Channel.QueueDeleteAsync(queue.Name, false, true);
    }

    private async Task FlushAndSortQueue(string queue)
    {
        while (true)
        {
            var getResult = await _rabbitMqChannel!.Channel.BasicGetAsync(queue, false);
            if (getResult == null)
                break;

            var transaction = V1.Transaction.Parser.ParseFrom(getResult.Body.Span);
            var queueName = _queueResolver.GetQueueName(transaction);

            await _rabbitMqChannel.Channel.BasicPublishAsync("", queueName, getResult.Body);
            await _rabbitMqChannel.Channel.BasicAckAsync(getResult.DeliveryTag, false);
        }
    }

    public override void Dispose()
    {
        if (_rabbitMqChannel is not null)
            _rabbitMqChannel!.Dispose();

        base.Dispose();
    }
}
