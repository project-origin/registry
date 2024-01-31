using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.Registry.Server.Interfaces;
using ProjectOrigin.Registry.Server.Options;

namespace ProjectOrigin.Registry.Server.Services;

public class VerifyTransactionManager : IHostedService
{
    private readonly List<VerifyTransactionWorker> _workers = new List<VerifyTransactionWorker>();
    private readonly IRabbitMqChannelPool _channelPool;
    private readonly ProcessOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly IQueueResolver _queueResolver;

    public VerifyTransactionManager(IRabbitMqChannelPool channelPool, IOptions<ProcessOptions> options, IServiceProvider serviceProvider)
    {
        _channelPool = channelPool;
        _options = options.Value;
        _serviceProvider = serviceProvider;
        _queueResolver = new ConsistentHashRingQueueResolver(options);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        for (int i = 0; i < _options.VerifyThreads; i++)
        {
            var logger = _serviceProvider.GetRequiredService<ILogger<VerifyTransactionWorker>>();
            var queueName = _queueResolver.GetQueueName(_options.ServerNumber, i);
            var transactionVerifier = _serviceProvider.GetRequiredService<VerifyTransactionConsumer>();
            var queueResolver = _serviceProvider.GetRequiredService<IQueueResolver>();

            var worker = new VerifyTransactionWorker(
                logger,
                _channelPool.GetChannel(),
                queueName,
                transactionVerifier,
                queueResolver);

            _workers.Add(worker);
        }

        await Task.WhenAll(_workers.Select(x => x.StartAsync(cancellationToken)));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var worker in _workers)
        {
            worker.Dispose();
        }

        await Task.CompletedTask;
    }
}
