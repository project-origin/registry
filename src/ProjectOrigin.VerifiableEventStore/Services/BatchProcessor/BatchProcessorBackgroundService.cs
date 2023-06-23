using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;
using ProjectOrigin.VerifiableEventStore.Services.TransactionStatusCache;

namespace ProjectOrigin.VerifiableEventStore.Services.BatchProcessor;

public class BatchProcessorBackgroundService : BackgroundService
{
    private readonly TimeSpan _period = TimeSpan.FromSeconds(5);
    private readonly ILogger<BatchProcessorBackgroundService> _logger;
    private readonly IEventStore _eventStore;
    private readonly IBlockchainConnector _blockchainConnector;
    private readonly ITransactionStatusService _statusService;

    public BatchProcessorBackgroundService(ILogger<BatchProcessorBackgroundService> logger, IEventStore eventStore, IBlockchainConnector blockchainConnector, ITransactionStatusService statusService)
    {
        _logger = logger;
        _eventStore = eventStore;
        _blockchainConnector = blockchainConnector;
        _statusService = statusService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_period);
        var processer = new BatchProcessorJob(_blockchainConnector, _eventStore, _statusService);

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            _logger.LogInformation("Executing BatchProcesser");

            await processer.Execute(stoppingToken);

            _logger.LogInformation("Executed BatchProcesser");
        }
    }
}
