using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.VerifiableEventStore.Tests;

public class MemoryEventStoreTests
{
    [Fact]
    public async Task MemoryEventStore_StoreEvents_ReturnsBatch()
    {
        var batch1 = new Fixture().Create<Batch>();
        var batch2 = new Fixture().Create<Batch>();

        var memoryEventStore = new MemoryEventStore();

        await memoryEventStore.StoreBatch(batch1);
        await memoryEventStore.StoreBatch(batch2);

        var batchResult = await memoryEventStore.GetBatch(batch1.Events.First().Id);

        Assert.Equal(batch1, batchResult);
    }

    [Fact]
    public async Task MemoryEventStore_GetBatchNotFound_ReturnNull()
    {
        var memoryEventStore = new MemoryEventStore();
        var eventId = new Fixture().Create<EventId>();
        var batchResult = await memoryEventStore.GetBatch(eventId);

        Assert.Null(batchResult);
    }
}
