using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.BatchPublisher;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;
using ProjectOrigin.VerifiableEventStore.Services.TransactionStatusCache;

namespace ProjectOrigin.VerifiableEventStore.Services.BatchProcessor;

public sealed class BatchProcessorJob
{
    public static Meter Meter = new("Registry.BatchProcessor");
    public static Counter<long> BatchCounter = Meter.CreateCounter<long>("batch_processor.batches_processed");
    public static Counter<long> TransactionCounter = Meter.CreateCounter<long>("batch_processor.transactions_processed");
    public static Histogram<long> BatchProcessingTime = Meter.CreateHistogram<long>("batch_processor.milliseconds_per_batch");

    private readonly ILogger<BatchProcessorJob> _logger;
    private readonly IBatchPublisher _publisher;
    private readonly IEventStore _eventStore;
    private readonly ITransactionStatusService _statusService;

    public BatchProcessorJob(
        ILogger<BatchProcessorJob> logger,
        IBatchPublisher blockchainConnector,
        IEventStore eventStore,
        ITransactionStatusService statusService)
    {
        _logger = logger;
        _publisher = blockchainConnector;
        _eventStore = eventStore;
        _statusService = statusService;
    }

    public async Task Execute(CancellationToken stoppingToken)
    {
        Stopwatch sw = new();
        sw.Start();

        var (batchHeader, transactionHashes) = await _eventStore.CreateNextBatch();
        if (batchHeader is null)
        {
            _logger.LogDebug("No transactions to batch");
            return;
        }

        var publication = await _publisher.PublishBatch(batchHeader);
        await _eventStore.FinalizeBatch(BatchHash.FromHeader(batchHeader), publication);

        foreach (var transactionHash in transactionHashes)
        {
            await _statusService.SetTransactionStatus(transactionHash, new TransactionStatusRecord(TransactionStatus.Committed));
        }

        sw.Stop();
        _logger.LogDebug($"Published new batch with {transactionHashes.Count()} transactions in {sw.ElapsedMilliseconds}ms");
        BatchCounter.Add(1);
        TransactionCounter.Add(transactionHashes.Count());
        BatchProcessingTime.Record(sw.ElapsedMilliseconds);
    }
}
