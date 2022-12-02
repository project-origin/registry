using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.EventStore.Postgres;

namespace ProjectOrigin.VerifiableEventStore.Tests
{
    public class PostgresqlEventStoreTests : IClassFixture<DatabaseFixture>
    {
        private readonly PostgresqlEventStore _eventStore;

        public PostgresqlEventStoreTests(DatabaseFixture fixture)
        {
            var storeOptions = new PostgresqlEventStoreOptions
            {
                ConnectionString = fixture.Database.ConnectionString,
                CreateSchema = true
            };
            _eventStore = new PostgresqlEventStore(storeOptions);
        }

        [Fact]
        public async Task PostgresqlEventStoreEventStore_StoreEvents_ReturnsBatch()
        {
            var fixture = new Fixture();
            const int NUMBER_OF_EVENTS = 10000;
            var batches = new List<Batch>();
            for (var i = 0; i < NUMBER_OF_EVENTS; i++)
            {
                var events = new List<VerifiableEvent>();
                var streamId = Guid.NewGuid();
                for (var index = 0; index < 13; index++)
                {
                    events.Add(new VerifiableEvent(new EventId(streamId, index), fixture.Create<byte[]>()));
                }

                var batch = new Batch(fixture.Create<string>(), fixture.Create<string>(), events);
                batches.Add(batch);
            }

            foreach (var item in batches)
            {
                await _eventStore.StoreBatch(item);
            }

            var firstBatch = batches[0];
            var eventStream = await _eventStore.GetEventsForEventStream(firstBatch.Events[0].Id.EventStreamId);
            Assert.NotNull(eventStream);
            Assert.Equal(firstBatch.Events.Count, eventStream.Count());

            var batchResult = await _eventStore.GetBatch(firstBatch.Events.First().Id);
            Assert.NotNull(batchResult);
            Assert.NotEmpty(batchResult.Events);
        }

        [Fact]
        public async Task Can_InsertEvent_In_LoopAsync()
        {
            var fixture = new Fixture();
            const int NUMBER_OF_EVENTS = 150;
            var eventId = new EventId(Guid.NewGuid(), 0);
            for (var i = 0; i < NUMBER_OF_EVENTS; i++)
            {
                eventId = new EventId(Guid.NewGuid(), 0);
                var @event = new VerifiableEvent(eventId, fixture.Create<byte[]>());
                await _eventStore.Store(@event);
            }
            var events = await _eventStore.GetEventsForEventStream(eventId.EventStreamId);
            Assert.Single(events);
            var batch = await _eventStore.GetBatch(eventId);
            Assert.NotNull(batch);
        }

        [Fact]
        public async Task Can_Insert_Many_Events_On_Same_Stream_LoopAsync()
        {
            var fixture = new Fixture();
            const int NUMBER_OF_EVENTS = 150;
            var streamId = Guid.NewGuid();
            for (var i = 0; i < NUMBER_OF_EVENTS; i++)
            {
                var eventId = new EventId(streamId, i);
                var @event = new VerifiableEvent(eventId, fixture.Create<byte[]>());
                await _eventStore.Store(@event);
            }
            var events = await _eventStore.GetEventsForEventStream(streamId);
            Assert.NotEmpty(events);
            Assert.Equal(NUMBER_OF_EVENTS, events.Count());
        }

        [Fact]
        public async Task Can_insert_eventAsync()
        {
            var fixture = new Fixture();
            var @event = new VerifiableEvent(new EventId(Guid.NewGuid(), 0), fixture.Create<byte[]>());

            await _eventStore.Store(@event);

            var eventStream = await _eventStore.GetEventsForEventStream(@event.Id.EventStreamId);
            var fromDatabase = eventStream.First();
            Assert.NotNull(fromDatabase);
            Assert.Equal(@event.Id.EventStreamId, fromDatabase.Id.EventStreamId);
            Assert.Equal(@event.Id.Index, fromDatabase.Id.Index);
            Assert.Equal(@event.Content, fromDatabase.Content);
        }

        [Fact]
        public async Task Will_Throw_Exception_When_Index_Is_Out_Of_Order()
        {
            var fixture = new Fixture();
            var @event = new VerifiableEvent(new EventId(Guid.NewGuid(), 99), fixture.Create<byte[]>());
            async Task act() => await _eventStore.Store(@event);
            await Assert.ThrowsAnyAsync<Exception>(act);
        }
    }
}
