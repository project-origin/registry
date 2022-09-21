using EnergyOrigin.VerifiableEventStore.Api.Services.BlockchainConnector;
using EnergyOrigin.VerifiableEventStore.Api.Services.EventStore;
using Microsoft.Extensions.Options;

namespace EnergyOrigin.VerifiableEventStore.Api.Services.Batcher;

public class MemoryBatcher : IBatcher
{
    private IBlockchainConnector blockchainConnector;
    private IEventStore eventStore;
    private IOptions<BatcherOptions> options;
    private List<Event> events = new List<Event>();
    private long batchSize;

    public MemoryBatcher(IBlockchainConnector blockchainConnector, IEventStore eventStore, IOptions<BatcherOptions> options)
    {
        this.blockchainConnector = blockchainConnector;
        this.eventStore = eventStore;
        this.options = options;

        this.batchSize = (long)Math.Pow(2, options.Value.BatchSize);
    }

    public async Task PublishEvent(PublishEventRequest request)
    {
        events.Add(new(request.EventId, request.EventData));

        if (events.Count >= batchSize)
        {
            var batchEvents = events;
            events = new List<Event>();
            var root = CalculateMerkleRoot(events);
            var transaction = await blockchainConnector.PublishBytes(root);

            Block? block = await blockchainConnector.GetBlock(transaction);
            while (block == null || !block.Final)
            {
                await Task.Delay(1000);
                block = await blockchainConnector.GetBlock(transaction);
            }

            var batch = new Batch(block.BlockId, transaction.TransactionId, batchEvents);

            await eventStore.StoreBatch(batch);
        }
    }

    private byte[] CalculateMerkleRoot(List<Event> events)
    {
        return null;
    }
}
