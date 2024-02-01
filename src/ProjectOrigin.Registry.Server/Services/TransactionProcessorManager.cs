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

public class TransactionProcessorManager : IHostedService
{
    private readonly List<TransactionProcessorWorker> _workers = new List<TransactionProcessorWorker>();
    private readonly TransactionProcessorOptions _options;
    private readonly IRabbitMqChannelPool _channelPool;
    private readonly IServiceProvider _serviceProvider;
    private readonly IQueueResolver _queueResolver;

    public TransactionProcessorManager(
        IOptions<TransactionProcessorOptions> options,
        IRabbitMqChannelPool channelPool,
        IServiceProvider serviceProvider,
        IQueueResolver queueResolver)
    {
        _options = options.Value;
        _channelPool = channelPool;
        _serviceProvider = serviceProvider;
        _queueResolver = queueResolver;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        for (int i = 0; i < _options.Threads; i++)
        {
            var logger = _serviceProvider.GetRequiredService<ILogger<TransactionProcessorWorker>>();
            var queueName = _queueResolver.GetQueueName(_options.ServerNumber, i);
            var transactionVerifier = _serviceProvider.GetRequiredService<TransactionProcessor>();
            var queueResolver = _serviceProvider.GetRequiredService<IQueueResolver>();

            var worker = new TransactionProcessorWorker(
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
