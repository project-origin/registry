using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.Registry.Options;

namespace ProjectOrigin.Registry.BlockFinalizer.Process;

public class BlockFinalizerBackgroundService : BackgroundService
{
    private readonly TimeSpan _blockInterval;
    private readonly ILogger<BlockFinalizerBackgroundService> _logger;
    private readonly IBlockFinalizer _finalizer;

    public BlockFinalizerBackgroundService(ILogger<BlockFinalizerBackgroundService> logger, IBlockFinalizer finalizer, IOptions<BlockFinalizationOptions> options)
    {
        _logger = logger;
        _finalizer = finalizer;
        _blockInterval = options.Value.Interval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_blockInterval);

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                _logger.LogTrace("Executing BlockFinalizer");
                await _finalizer.Execute(stoppingToken);
                _logger.LogTrace("Executed BlockFinalizer");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error executing BlockFinalizer");
            }
        }
    }
}
