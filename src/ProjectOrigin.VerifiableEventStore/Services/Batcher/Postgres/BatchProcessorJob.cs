using ProjectOrigin.VerifiableEventStore.Extensions;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.VerifiableEventStore.Services.Batcher.Postgres;

public sealed class BatchProcessorJob
{
    private readonly IBlockchainConnector _blockchainConnector;
    private readonly IEventStore _eventStore;
    private readonly TimeSpan _period = TimeSpan.FromSeconds(30);
    private readonly int _numberOf_Batches = 10;

    public BatchProcessorJob(IBlockchainConnector blockchainConnector, IEventStore eventStore)
    {
        _blockchainConnector = blockchainConnector;
        _eventStore = eventStore;
    }

    public async Task Execute(CancellationToken stoppingToken)
    {
        // go check if we have any batches that are full
        // If we have any - then go get the batch and the events
        var batches = await _eventStore.GetBatchesForFinalization(_numberOf_Batches);
        foreach (var item in batches)
        {
            var events = await _eventStore.GetEventsForBatch(item);
            var merkleRoot = events.CalculateMerkleRoot(x => x.Content);

            var transaction = await _blockchainConnector.PublishBytes(merkleRoot);

            // This should be in another service
            var block = await _blockchainConnector.GetBlock(transaction);
            while (block == null || !block.Final)
            {
                await Task.Delay(_period, stoppingToken);
                block = await _blockchainConnector.GetBlock(transaction);
            }

            // Update batch in EventStore
            await _eventStore.FinalizeBatch(item, block.BlockId, transaction.TransactionHash);
        }

        return;
    }
}
