using System;
using System.Linq;
using System.Threading.Tasks;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.VerifiableEventStore.Tests;

public abstract class AbstractEventStoreTests<T> where T : IEventStore
{
    protected const int BatchExponent = 4;
    protected Fixture _fixture;
    protected abstract T EventStore { get; }

    public AbstractEventStoreTests()
    {
        _fixture = new Fixture();
    }

    private VerifiableEvent CreateFakeEvent(Guid stream, int index)
    {
        var eventId = new EventId(stream, index);
        return new VerifiableEvent(eventId, _fixture.Create<string>(), _fixture.Create<byte[]>());
    }

    [Fact]
    public async Task can_insert_events()
    {
        const int NUMBER_OF_EVENTS = 150;
        Guid streamId = Guid.Empty;
        for (var i = 0; i < NUMBER_OF_EVENTS; i++)
        {
            streamId = Guid.NewGuid();
            var @event = CreateFakeEvent(streamId, 0);
            await EventStore.Store(@event);
        }

        var events = await EventStore.GetEventsForEventStream(streamId);
        Assert.Single(events);

        var batch = await EventStore.GetBatchFromEventId(new EventId(streamId, 0));
        Assert.NotNull(batch);
    }

    [Fact]
    public async Task Can_Insert_Many_Events_On_Same_Stream_LoopAsync()
    {
        const int NUMBER_OF_EVENTS = 1500;
        var streamId = Guid.NewGuid();
        for (var i = 0; i < NUMBER_OF_EVENTS; i++)
        {
            var @event = CreateFakeEvent(streamId, i);
            await EventStore.Store(@event);
        }
        var events = await EventStore.GetEventsForEventStream(streamId);
        Assert.NotEmpty(events);
        Assert.Equal(NUMBER_OF_EVENTS, events.Count());
    }

    [Fact]
    public async Task cannot_insert_events_out_of_order_on_same_stream()
    {
        var streamId = Guid.NewGuid();

        await EventStore.Store(CreateFakeEvent(streamId, 0));
        var @event = CreateFakeEvent(streamId, 3);

        async Task act() => await EventStore.Store(@event);
        await Assert.ThrowsAnyAsync<OutOfOrderException>(act);
    }

    [Fact]
    public async Task Will_Throw_Exception_When_Index_Is_Out_Of_Order()
    {
        var streamId = Guid.NewGuid();

        var @event = CreateFakeEvent(streamId, 99);

        async Task act() => await EventStore.Store(@event);
        await Assert.ThrowsAnyAsync<OutOfOrderException>(act);
    }

    [Fact]
    public async Task Will_Store_EventAsync()
    {
        // Given
        var @event = CreateFakeEvent(Guid.NewGuid(), 0);

        // When
        await EventStore.Store(@event);

        // Then
        var eventStream = await EventStore.GetEventsForEventStream(@event.Id.EventStreamId);
        Assert.NotEmpty(eventStream);
    }

    [Fact]
    public async Task MemoryEventStore_GetBatchNotFound_ReturnNull()
    {
        var eventId = new Fixture().Create<EventId>();
        var batchResult = await EventStore.GetBatchFromEventId(eventId);

        Assert.Null(batchResult);
    }

    [Fact]
    public async Task Can_Get_Batches_For_Finalization()
    {
        const int NUMBER_OF_EVENTS = 1500;
        for (var i = 0; i < NUMBER_OF_EVENTS; i++)
        {
            var @event = CreateFakeEvent(Guid.NewGuid(), 0);
            await EventStore.Store(@event);
        }

        var batchFound = await EventStore.TryGetNextBatchForFinalization(out var batch);

        Assert.True(batchFound);
        Assert.NotNull(batch);
    }

    [Fact]
    public async Task Can_FinalizeBatch()
    {
        const int NUMBER_OF_EVENTS = 1500;
        for (var i = 0; i < NUMBER_OF_EVENTS; i++)
        {
            var @event = CreateFakeEvent(Guid.NewGuid(), 0);
            await EventStore.Store(@event);
        }

        var batchFound = await EventStore.TryGetNextBatchForFinalization(out var batch);
        Assert.True(batchFound);
        Assert.False(batch.IsFinalized);

        await EventStore.FinalizeBatch(batch.Id, _fixture.Create<string>(), _fixture.Create<string>());
        var b2 = await EventStore.GetBatch(batch.Id);

        Assert.NotNull(b2);
        Assert.True(b2.IsFinalized);
    }

    [Fact]
    public async Task Can_insert_eventAsync()
    {
        var @event = CreateFakeEvent(Guid.NewGuid(), 0);

        await EventStore.Store(@event);

        var eventStream = await EventStore.GetEventsForEventStream(@event.Id.EventStreamId);
        var fromDatabase = eventStream.First();

        Assert.NotNull(fromDatabase);
        Assert.Equal(@event.Id.EventStreamId, fromDatabase.Id.EventStreamId);
        Assert.Equal(@event.Id.Index, fromDatabase.Id.Index);
        Assert.Equal(@event.Content, fromDatabase.Content);
    }
}
