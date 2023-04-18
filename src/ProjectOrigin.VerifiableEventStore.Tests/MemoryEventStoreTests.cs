using AutoFixture;
using Microsoft.Extensions.Options;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.BatchProcessor;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.VerifiableEventStore.Tests;

public class MemoryEventStoreTests
{
    private readonly Fixture _fixture;
    public MemoryEventStoreTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public async Task MemoryEventStore_StoreEvents_ReturnsBatch()
    {
        var options = new VerifiableEventStoreOptions() { BatchSizeExponent = 0 };
        var memoryEventStore = new MemoryEventStore(Options.Create(options));
        var @event1 = _fixture.Create<VerifiableEvent>();
        var @event2 = _fixture.Create<VerifiableEvent>();
        await memoryEventStore.Store(@event1);
        await memoryEventStore.Store(@event2);

        var batchIds = await memoryEventStore.GetBatchesForFinalization(10);
        var batch = await memoryEventStore.GetEventsForBatch(batchIds.First());

        Assert.NotEmpty(batch);
    }

    [Fact]
    public async Task Will_Store_EventAsync()
    {
        // Given
        var options = new VerifiableEventStoreOptions() { BatchSizeExponent = 0 };
        var memoryEventStore = new MemoryEventStore(Options.Create(options));
        var @event = _fixture.Create<VerifiableEvent>();
        // When
        await memoryEventStore.Store(@event);
        // Then
        var eventStream = await memoryEventStore.GetEventsForEventStream(@event.Id.EventStreamId);
        Assert.NotEmpty(eventStream);
    }

    [Fact]
    public async Task MemoryEventStore_GetBatchNotFound_ReturnNull()
    {
        var options = new VerifiableEventStoreOptions() { BatchSizeExponent = 0 };
        var memoryEventStore = new MemoryEventStore(Options.Create(options));
        var eventId = new Fixture().Create<EventId>();
        var batchResult = await memoryEventStore.GetBatch(eventId);

        Assert.Null(batchResult);
    }
}
