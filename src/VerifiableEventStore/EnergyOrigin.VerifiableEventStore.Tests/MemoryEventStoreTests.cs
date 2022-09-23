using EnergyOrigin.VerifiableEventStore.Api.Services.EventStore;

namespace EnergyOrigin.VerifiableEventStore.Tests;

public class MemoryEventStoreTests
{
    [Fact]
    public async Task MemoryEventStore_StoreEvents_ReturnsBatch()
    {
        var batch1 = new Fixture().Create<Batch>();
        var batch2 = new Fixture().Create<Batch>();

        var meroryEventStore = new MemoryEventStore();

        await meroryEventStore.StoreBatch(batch1);
        await meroryEventStore.StoreBatch(batch2);

        var batchResult = await meroryEventStore.GetBatch(batch1.Events.First().Id);

        Assert.Equal(batch1, batchResult);
    }

    [Fact]
    public async Task MemoryEventStore_GetBatchNotFound_ReturnNull()
    {
        var meroryEventStore = new MemoryEventStore();
        var batchResult = await meroryEventStore.GetBatch(Guid.NewGuid());

        Assert.Null(batchResult);
    }
}
