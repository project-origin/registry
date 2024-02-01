using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Registry.Server.Extensions;
using ProjectOrigin.Registry.Server.Interfaces;
using ProjectOrigin.Registry.Server.Models;
using RabbitMQ.Client;

namespace ProjectOrigin.Registry.Server.Services;

public sealed class QueueCleanupService : BackgroundService, IDisposable
{
    private readonly ILogger<QueueCleanupService> _logger;
    private readonly IRabbitMqHttpClient _rabbitMqHttpClient;
    private readonly IRabbitMqChannel _rabbitMqChannel;
    private readonly IQueueResolver _queueResolver;

    public QueueCleanupService(
        ILogger<QueueCleanupService> logger,
        IRabbitMqHttpClient rabbitMqHttpClient,
        IRabbitMqChannel rabbitMqChannel,
        IQueueResolver queueResolver)
    {
        _logger = logger;
        _rabbitMqHttpClient = rabbitMqHttpClient;
        _rabbitMqChannel = rabbitMqChannel;
        _queueResolver = queueResolver;
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
        await _rabbitMqChannel.Channel.QueueDeleteAsync(queue.Name, false, true);
    }

    private async Task FlushAndSortQueue(string queue)
    {
        while (true)
        {
            var getResult = await _rabbitMqChannel.Channel.BasicGetAsync(queue, false);
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
        base.Dispose();
        _rabbitMqChannel.Dispose();
    }
}
