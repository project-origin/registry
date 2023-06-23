using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.BatchProcessor;
using ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;
using ProjectOrigin.VerifiableEventStore.Services.TransactionStatusCache;

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
        var iTransactionService = new Mock<ITransactionStatusService>();
        var batcherOptions = new VerifiableEventStoreOptions() { BatchSizeExponent = batchExponent };
        var eventStore = new MemoryEventStore(Options.Create(batcherOptions));
        var eventsPerBatch = 1 << batchExponent;

        for (int i = 0; i < numberOfEvents; i++)
        {
            var @event = new VerifiableEvent(new EventId(Guid.NewGuid(), 0), fixture.Create<string>(), fixture.Create<byte[]>());
            await eventStore.Store(@event);
        }

        var job = new BatchProcessorJob(blockChainConnector.Object, eventStore, iTransactionService.Object);
        blockChainConnector.Setup(obj => obj.PublishBytes(It.IsAny<byte[]>())).Returns(Task.FromResult(new TransactionReference(transactionId)));
        blockChainConnector.Setup(obj => obj.GetBlock(It.IsAny<TransactionReference>())).Returns(Task.FromResult<Block?>(new Block(blockId, true)));

        // When
        await job.Execute(CancellationToken.None);

        // Then
        blockChainConnector.Verify(obj => obj.PublishBytes(It.IsAny<byte[]>()), Times.Exactly(expectedBatches));
        iTransactionService.Verify(obj => obj.SetTransactionStatus(It.IsAny<string>(), It.Is<TransactionStatusRecord>(x => x.NewStatus == TransactionStatus.Committed && string.IsNullOrEmpty(x.Message))),
            Times.Exactly(eventsPerBatch * expectedBatches));

        var result = await eventStore.TryGetNextBatchForFinalization(out var batch);
        Assert.False(result);
        Assert.Null(batch);
    }
}
