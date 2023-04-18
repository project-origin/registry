using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.BatchProcessor;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.VerifiableEventStore.Tests;

public class BatchProcessorJobTests
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 0, 1)]
    [InlineData(2, 1, 1)]
    [InlineData(4, 1, 2)]
    [InlineData(8, 1, 4)]
    [InlineData(9, 1, 4)]
    [InlineData(10, 5, 0)]
    [InlineData(16, 5, 0)]
    [InlineData(32, 5, 1)]
    [InlineData(64, 5, 2)]
    public async Task Will_Finalize_Full_Batch(int numberOfEvents, int batchExponent, int expectedBatches)
    {
        var fixture = new Fixture();
        var blockId = fixture.Create<string>();
        var transactionId = fixture.Create<string>();
        var blockChainConnector = new Mock<IBlockchainConnector>();
        var batcherOptions = new VerifiableEventStoreOptions() { BatchSizeExponent = batchExponent };
        var eventStore = new MemoryEventStore(Options.Create(batcherOptions));

        for (int i = 0; i < numberOfEvents; i++)
        {
            var @event = fixture.Create<VerifiableEvent>();
            await eventStore.Store(@event);
        }

        var job = new BatchProcessorJob(blockChainConnector.Object, eventStore);
        blockChainConnector.Setup(obj => obj.PublishBytes(It.IsAny<byte[]>())).Returns(Task.FromResult(new TransactionReference(transactionId)));
        blockChainConnector.Setup(obj => obj.GetBlock(It.IsAny<TransactionReference>())).Returns(Task.FromResult<Block?>(new Block(blockId, true)));

        // When
        await job.Execute(CancellationToken.None);

        // Then
        blockChainConnector.Verify(obj => obj.PublishBytes(It.IsAny<byte[]>()), Times.Exactly(expectedBatches));

        var result = await eventStore.GetBatchesForFinalization(10);
        Assert.Empty(result);
    }
}
