using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.Batcher;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.VerifiableEventStore.Tests;

public class MemoryBatcherTests
{
    [Fact]
    public async Task MemoryBatcher_WhenBatchIsFull_BatchIsStoredAndPublished()
    {
        var blockId = new Fixture().Create<string>();
        var transactionId = new Fixture().Create<string>();
        var event1 = new Fixture().Create<VerifiableEvent>();
        var event2 = new Fixture().Create<VerifiableEvent>();

        var rootHash = CalculateRoot(event1.Content, event2.Content);

        var optionsMock = new Mock<IOptions<BatcherOptions>>();
        optionsMock.Setup(obj => obj.Value).Returns(new BatcherOptions { BatchSizeExponent = 1 });

        var eventStoreMock = new Mock<IEventStore>();

        var blockchainMock = new Mock<IBlockchainConnector>();
        blockchainMock.Setup(obj => obj.PublishBytes(It.Is<byte[]>(b => b[0] == rootHash[0]))).Returns(Task.FromResult(new TransactionReference(transactionId)));
        blockchainMock.Setup(obj => obj.GetBlock(It.IsAny<TransactionReference>())).Returns(Task.FromResult<Block?>(new Block(blockId, true)));


        var batcher = new MemoryBatcher(blockchainMock.Object, eventStoreMock.Object, optionsMock.Object);

        await batcher.PublishEvent(event1);
        await batcher.PublishEvent(event2);

        eventStoreMock.Verify(obj => obj.StoreBatch(It.Is<Batch>(b => b.BlockId == blockId && b.TransactionId == transactionId && b.Events.Count == 2)), Times.Once);
        blockchainMock.Verify(obj => obj.PublishBytes(It.Is<byte[]>(b => b[0] == rootHash[0])), Times.Once);
    }

    private byte[] CalculateRoot(byte[] left, byte[] right)
    {
        var shaLeft = SHA256.HashData(left);
        var shaRight = SHA256.HashData(right);

        return SHA256.HashData(shaLeft.Concat(shaRight).ToArray());
    }
}
