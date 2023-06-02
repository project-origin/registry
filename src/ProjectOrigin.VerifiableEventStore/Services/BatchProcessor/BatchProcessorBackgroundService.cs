using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.VerifiableEventStore.Services.BatchProcessor;

public class BatchProcessorBackgroundService : BackgroundService
{
    private readonly TimeSpan _period = TimeSpan.FromSeconds(30);
    private readonly ILogger<BatchProcessorBackgroundService> _logger;
    private readonly IEventStore _eventStore;
    private readonly IBlockchainConnector _blockchainConnector;

    public BatchProcessorBackgroundService(ILogger<BatchProcessorBackgroundService> logger, IEventStore eventStore, IBlockchainConnector blockchainConnector)
    {
        _logger = logger;
        _eventStore = eventStore;
        _blockchainConnector = blockchainConnector;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_period);

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            _logger.LogInformation("Executing BatchProcesser");

            var processer = new BatchProcessorJob(_blockchainConnector, _eventStore);
            await processer.Execute(stoppingToken);

            _logger.LogInformation("Executed BatchProcesser");
        }
    }
}
