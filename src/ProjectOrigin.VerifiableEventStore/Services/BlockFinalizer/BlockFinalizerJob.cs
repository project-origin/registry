using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.BlockPublisher;
using ProjectOrigin.VerifiableEventStore.Services.Repository;
using ProjectOrigin.VerifiableEventStore.Services.TransactionStatusCache;

namespace ProjectOrigin.VerifiableEventStore.Services.BlockFinalizer;

public sealed class BlockFinalizerJob
{
    public static Meter Meter = new("Registry.BlockFinalizer");
    public static Counter<long> BlockCounter = Meter.CreateCounter<long>("block_finalizer.blocks_processed");
    public static Counter<long> TransactionCounter = Meter.CreateCounter<long>("block_finalizer.transactions_processed");
    public static Histogram<long> BlockTime = Meter.CreateHistogram<long>("block_finalizer.milliseconds_per_block");

    private readonly ILogger<BlockFinalizerJob> _logger;
    private readonly IBlockPublisher _publisher;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionStatusService _statusService;

    public BlockFinalizerJob(
        ILogger<BlockFinalizerJob> logger,
        IBlockPublisher blockchainConnector,
        ITransactionRepository eventStore,
        ITransactionStatusService statusService)
    {
        _logger = logger;
        _publisher = blockchainConnector;
        _transactionRepository = eventStore;
        _statusService = statusService;
    }

    public async Task Execute(CancellationToken stoppingToken)
    {
        Stopwatch sw = new();
        sw.Start();

        var newBlock = await _transactionRepository.CreateNextBlock();
        if (newBlock is null)
        {
            _logger.LogInformation("No transactions to put in block");
            return;
        }

        var publication = await _publisher.PublishBlock(newBlock.Header);
        await _transactionRepository.FinalizeBlock(BlockHash.FromHeader(newBlock.Header), publication);

        foreach (var transactionHash in newBlock.TransactionHashes)
        {
            await _statusService.SetTransactionStatus(transactionHash, new TransactionStatusRecord(TransactionStatus.Committed));
        }

        sw.Stop();
        _logger.LogInformation($"Published new block with {newBlock.TransactionHashes.Count()} transactions in {sw.ElapsedMilliseconds}ms");
        BlockCounter.Add(1);
        TransactionCounter.Add(newBlock.TransactionHashes.Count());
        BlockTime.Record(sw.ElapsedMilliseconds);
    }
}
