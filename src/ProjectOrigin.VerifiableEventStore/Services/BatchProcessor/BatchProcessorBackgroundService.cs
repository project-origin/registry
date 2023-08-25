using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectOrigin.VerifiableEventStore.Services.BatchPublisher;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;
using ProjectOrigin.VerifiableEventStore.Services.TransactionStatusCache;

namespace ProjectOrigin.VerifiableEventStore.Services.BatchProcessor;

public class BatchProcessorBackgroundService : BackgroundService
{
    private readonly TimeSpan _period = TimeSpan.FromSeconds(5);
    private readonly ILogger<BatchProcessorBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public BatchProcessorBackgroundService(ILogger<BatchProcessorBackgroundService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var processor = new BatchProcessorJob(
            _serviceProvider.GetRequiredService<ILogger<BatchProcessorJob>>(),
            _serviceProvider.GetRequiredService<IBatchPublisher>(),
            _serviceProvider.GetRequiredService<IEventStore>(),
            _serviceProvider.GetRequiredService<ITransactionStatusService>()
            );

        using var timer = new PeriodicTimer(_period);

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            _logger.LogTrace("Executing BatchProcesser");

            await processor.Execute(stoppingToken);

            _logger.LogTrace("Executed BatchProcesser");
        }
    }
}
