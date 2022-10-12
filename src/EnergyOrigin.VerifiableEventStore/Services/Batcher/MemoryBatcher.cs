using EnergyOrigin.VerifiableEventStore.Extensions;
using EnergyOrigin.VerifiableEventStore.Models;
using EnergyOrigin.VerifiableEventStore.Services.BlockchainConnector;
using EnergyOrigin.VerifiableEventStore.Services.EventStore;
using Microsoft.Extensions.Options;

namespace EnergyOrigin.VerifiableEventStore.Services.Batcher;

public class MemoryBatcher : IBatcher
{
    private IBlockchainConnector blockchainConnector;
    private IEventStore eventStore;
    private IOptions<BatcherOptions> options;
    private List<Event> events = new List<Event>();
    private long batchSize;
    private const int blockRetryWaitMilliseconds = 1000;

    public MemoryBatcher(IBlockchainConnector blockchainConnector, IEventStore eventStore, IOptions<BatcherOptions> options)
    {
        this.blockchainConnector = blockchainConnector;
        this.eventStore = eventStore;
        this.options = options;

        this.batchSize = (long)Math.Pow(2, options.Value.BatchSizeExponent);
    }

    public async Task PublishEvent(Event e)
    {
        events.Add(e);

        if (events.Count >= batchSize)
        {
            var batchEvents = events;
            events = new List<Event>();

            var batch = await PublishBatch(batchEvents);
            await eventStore.StoreBatch(batch);
        }
    }

    private async Task<Batch> PublishBatch(List<Event> batchEvents)
    {
        var root = batchEvents.CalculateMerkleRoot(x => x.Content);

        var transaction = await blockchainConnector.PublishBytes(root);

        Block? block = await blockchainConnector.GetBlock(transaction);
        while (block == null || !block.Final)
        {
            await Task.Delay(blockRetryWaitMilliseconds);
            block = await blockchainConnector.GetBlock(transaction);
        }

        var batch = new Batch(block.BlockId, transaction.TransactionHash, batchEvents);
        return batch;
    }
}