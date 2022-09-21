using System.Security.Cryptography;
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
    private const int blockRetryWaitMilliseconds = 1000;

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

            var batch = await PublishBatch(batchEvents);
            await eventStore.StoreBatch(batch);
        }
    }

    private async Task<Batch> PublishBatch(List<Event> batchEvents)
    {
        var root = CalculateMerkleRoot(events);

        var transaction = await blockchainConnector.PublishBytes(root);

        Block? block = await blockchainConnector.GetBlock(transaction);
        while (block == null || !block.Final)
        {
            await Task.Delay(blockRetryWaitMilliseconds);
            block = await blockchainConnector.GetBlock(transaction);
        }

        var batch = new Batch(block.BlockId, transaction.TransactionId, batchEvents);
        return batch;
    }

    private byte[] CalculateMerkleRoot(List<Event> events)
    {
        byte[] RecursiveShaNodes(IEnumerable<byte[]> nodes)
        {
            if (nodes.Count() == 1)
            {
                return nodes.Single();
            }

            List<byte[]> combined = new List<byte[]>();

            for (int i = 0; i < nodes.Count(); i = i + 2)
            {
                var left = SHA256.HashData(nodes.Skip(i).First());
                var right = SHA256.HashData(nodes.Skip(i + 1).First());

                combined.Add(SHA256.HashData(left.Concat(right).ToArray()));
            }

            return RecursiveShaNodes(combined);
        }

        if (!IsPowerOfTwo(events.Count))
        {
            throw new NotSupportedException("CalculateMerkleRoot currently only supported on exponents of 2");
        }

        return RecursiveShaNodes(events.Select(e => e.Content));
    }

    private bool IsPowerOfTwo(int x)
    {
        return (x & (x - 1)) == 0;
    }
}
