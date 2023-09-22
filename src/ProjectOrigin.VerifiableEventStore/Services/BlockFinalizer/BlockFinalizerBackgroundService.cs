using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.BlockPublisher;
using ProjectOrigin.VerifiableEventStore.Services.Repository;
using ProjectOrigin.VerifiableEventStore.Services.TransactionStatusCache;

namespace ProjectOrigin.VerifiableEventStore.Services.BlockFinalizer;

public class BlockFinalizerBackgroundService : BackgroundService
{
    private readonly TimeSpan _blockInterval;
    private readonly ILogger<BlockFinalizerBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public BlockFinalizerBackgroundService(ILogger<BlockFinalizerBackgroundService> logger, IServiceProvider serviceProvider, IOptions<BlockFinalizationOptions> options)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _blockInterval = options.Value.Interval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var processor = new BlockFinalizerJob(
            _serviceProvider.GetRequiredService<ILogger<BlockFinalizerJob>>(),
            _serviceProvider.GetRequiredService<IBlockPublisher>(),
            _serviceProvider.GetRequiredService<ITransactionRepository>(),
            _serviceProvider.GetRequiredService<ITransactionStatusService>()
            );

        using var timer = new PeriodicTimer(_blockInterval);

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                _logger.LogTrace("Executing BlockFinalizer");
                await processor.Execute(stoppingToken);
                _logger.LogTrace("Executed BlockFinalizer");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error executing BlockFinalizer");
            }
        }
    }
}
