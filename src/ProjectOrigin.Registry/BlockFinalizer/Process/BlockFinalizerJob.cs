using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Registry.BlockFinalizer.BlockPublisher;
using ProjectOrigin.Registry.Repository;
using ProjectOrigin.Registry.Repository.Models;
using ProjectOrigin.Registry.TransactionStatusCache;

namespace ProjectOrigin.Registry.BlockFinalizer.Process;

public sealed class BlockFinalizerJob : IBlockFinalizer
{
    public static readonly Meter Meter = new("Registry.BlockFinalizer");
    public static readonly Counter<long> BlockCounter = Meter.CreateCounter<long>("block_finalizer.blocks_processed");
    public static readonly Counter<long> TransactionCounter = Meter.CreateCounter<long>("block_finalizer.transactions_processed");
    public static readonly Histogram<long> BlockTime = Meter.CreateHistogram<long>("block_finalizer.milliseconds_per_block");

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
        var sw = Stopwatch.StartNew();

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
            await _statusService.SetTransactionStatus(transactionHash, new TransactionStatusRecord(TransactionStatus.Finalized));
        }

        sw.Stop();
        _logger.LogInformation("Published new block with {transactionCount} transactions in {elapsedMilliseconds}ms", newBlock.TransactionHashes.Count, sw.ElapsedMilliseconds);
        BlockCounter.Add(1);
        TransactionCounter.Add(newBlock.TransactionHashes.Count);
        BlockTime.Record(sw.ElapsedMilliseconds);
    }
}
