using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.VerifiableEventStore.Extensions;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.VerifiableEventStore.Services.Batcher;

public class MemoryBatcher : IBatcher
{
    private readonly IBlockchainConnector _blockchainConnector;
    private readonly IEventStore _eventStore;
    private List<VerifiableEvent> _events = new();
    private readonly long _batchSize;
    private readonly ILogger _logger;
    private const int BlockRetryWaitMilliseconds = 1000;

    public MemoryBatcher(IBlockchainConnector blockchainConnector, IEventStore eventStore, IOptions<BatcherOptions> options, ILogger<MemoryBatcher> logger)
    {
        _blockchainConnector = blockchainConnector;
        _eventStore = eventStore;
        _batchSize = (long)Math.Pow(2, options.Value.BatchSizeExponent);
        _logger = logger;
    }

    public async Task PublishEvent(VerifiableEvent e)
    {
        await _eventStore.Store(e);
    }

    private async Task<Batch> PublishBatch(List<VerifiableEvent> batchEvents)
    {
        var root = batchEvents.CalculateMerkleRoot(x => x.Content);

        var transaction = await _blockchainConnector.PublishBytes(root);

        var block = await _blockchainConnector.GetBlock(transaction);
        while (block == null || !block.Final)
        {
            await Task.Delay(BlockRetryWaitMilliseconds);
            block = await _blockchainConnector.GetBlock(transaction);
        }

        _logger.LogInformation($"Added transaction on blockchain with hash ”{transaction.TransactionHash}”");

        var batch = new Batch(block.BlockId, transaction.TransactionHash, batchEvents);
        return batch;
    }
}
