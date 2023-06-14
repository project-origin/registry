using System;
using System.Threading;
using System.Threading.Tasks;
using ProjectOrigin.VerifiableEventStore.Extensions;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;
using ProjectOrigin.VerifiableEventStore.Services.TransactionStatusCache;

namespace ProjectOrigin.VerifiableEventStore.Services.BatchProcessor;

public sealed class BatchProcessorJob
{
    private readonly IBlockchainConnector _blockchainConnector;
    private readonly IEventStore _eventStore;
    private readonly ITransactionStatusService _statusService;
    private readonly TimeSpan _period = TimeSpan.FromSeconds(5);

    public BatchProcessorJob(IBlockchainConnector blockchainConnector, IEventStore eventStore, ITransactionStatusService statusService)
    {
        _blockchainConnector = blockchainConnector;
        _eventStore = eventStore;
        _statusService = statusService;
    }

    public async Task Execute(CancellationToken stoppingToken)
    {
        // As long as there is batches to finalize continue finalizing.
        while (await _eventStore.TryGetNextBatchForFinalization(out var batch))
        {
            var events = await _eventStore.GetEventsForBatch(batch.Id);
            var merkleRoot = events.CalculateMerkleRoot(x => x.Content);

            var blockchainTransaction = await _blockchainConnector.PublishBytes(merkleRoot);

            // This should be in another service
            var block = await _blockchainConnector.GetBlock(blockchainTransaction);
            while (block == null || !block.Final)
            {
                await Task.Delay(_period, stoppingToken);
                block = await _blockchainConnector.GetBlock(blockchainTransaction);
            }

            // Update batch in EventStore
            await _eventStore.FinalizeBatch(batch.Id, block.BlockId, blockchainTransaction.TransactionHash);

            foreach (var e in events)
            {
                await _statusService.SetTransactionStatus(e.TransactionId, new Models.TransactionStatusRecord(TransactionStatus.Committed));
            }
        }
    }
}
