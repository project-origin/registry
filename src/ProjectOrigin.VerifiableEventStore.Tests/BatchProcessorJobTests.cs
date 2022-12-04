using System.Security.Cryptography;
using AutoFixture;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.Batcher;
using ProjectOrigin.VerifiableEventStore.Services.Batcher.Postgres;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.VerifiableEventStore.Tests;

public class BatchProcessorJobTests
{
    [Fact]
    public async Task Will_Finalize_Full_Batch()
    {
        var fixture = new Fixture();
        // Given
        var blockId = new Fixture().Create<string>();
        var transactionId = new Fixture().Create<string>();
        var blockChainConnector = new Mock<IBlockchainConnector>();
        var batcherOptions = new BatcherOptions() { BatchSizeExponent = 2 };
        var eventStore = new MemoryEventStore(batcherOptions);
        var event1 = new Fixture().Create<VerifiableEvent>();
        var event2 = new Fixture().Create<VerifiableEvent>();

        var rootHash = CalculateRoot(event1.Content, event2.Content);
        await eventStore.Store(event1);
        await eventStore.Store(event2);

        var job = new BatchProcessorJob(blockChainConnector.Object, eventStore);
        blockChainConnector.Setup(obj => obj.PublishBytes(It.Is<byte[]>(b => b[0] == rootHash[0]))).Returns(Task.FromResult(new TransactionReference(transactionId)));
        blockChainConnector.Setup(obj => obj.GetBlock(It.IsAny<TransactionReference>())).Returns(Task.FromResult<Block?>(new Block(blockId, true)));
        // When
        await job.Execute(CancellationToken.None);
        // Then
        var result = await eventStore.GetBatchesForFinalization();
        Assert.Empty(result);
    }

    private static byte[] CalculateRoot(byte[] left, byte[] right)
    {
        var shaLeft = SHA256.HashData(left);
        var shaRight = SHA256.HashData(right);

        return SHA256.HashData(shaLeft.Concat(shaRight).ToArray());
    }
}
