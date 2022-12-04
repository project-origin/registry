using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.VerifiableEventStore.Services.Batcher.Postgres;

public class BatchProcessorBackgroundService : BackgroundService
{
    private readonly TimeSpan _period = TimeSpan.FromSeconds(30);
    private readonly ILogger<BatchProcessorBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public BatchProcessorBackgroundService(ILogger<BatchProcessorBackgroundService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_period);

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            _logger.LogInformation("Executing BatchProcesser");


            using var scope = _serviceProvider.CreateScope();
            var blockchainConnector = scope.ServiceProvider.GetRequiredService<IBlockchainConnector>();
            var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

            var processer = new BatchProcessorJob(blockchainConnector, eventStore);
            await processer.Execute(stoppingToken);

            _logger.LogInformation("Executed BatchProcesser");
        }
    }
}
