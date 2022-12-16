using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.EventStore.Postgres;

namespace ProjectOrigin.VerifiableEventStore.Tests
{
    public class PostgresqlEventStoreTests : IDisposable, IClassFixture<DatabaseFixture>
    {
        private readonly PostgresqlEventStore _eventStore;

        public PostgresqlEventStoreTests(DatabaseFixture fixture)
        {
            var storeOptions = new PostgresqlEventStoreOptions
            {
                ConnectionString = fixture.Database.ConnectionString,
                CreateSchema = true,
                BatchExponent = 10
            };
            _eventStore = new PostgresqlEventStore(storeOptions);
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
            const int NUMBER_OF_EVENTS = 1500;
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
        public async Task Can_Get_Batches_For_Finalization()
        {
            var fixture = new Fixture();
            const int NUMBER_OF_EVENTS = 1500;
            var streamId = Guid.NewGuid();
            for (var i = 0; i < NUMBER_OF_EVENTS; i++)
            {
                var eventId = new EventId(streamId, i);
                var @event = new VerifiableEvent(eventId, fixture.Create<byte[]>());
                await _eventStore.Store(@event);
            }
            var batches = await _eventStore.GetBatchesForFinalization(10);
            Assert.NotEmpty(batches);
        }

        [Fact]
        public async Task Can_FinalizeBatch()
        {
            var fixture = new Fixture();
            const int NUMBER_OF_EVENTS = 1100;
            var streamId = Guid.NewGuid();
            for (var i = 0; i < NUMBER_OF_EVENTS; i++)
            {
                var eventId = new EventId(streamId, i);
                var @event = new VerifiableEvent(eventId, fixture.Create<byte[]>());
                await _eventStore.Store(@event);
            }
            var batches = await _eventStore.GetBatchesForFinalization(10);
            foreach (var batchId in batches)
            {
                await _eventStore.FinalizeBatch(batchId, fixture.Create<string>(), fixture.Create<string>());
            }
            var events = await _eventStore.GetEventsForBatch(batches.First());

            var batch = await _eventStore.GetBatch(events.First().Id);
            Assert.NotEqual(string.Empty, batch?.TransactionId);
            Assert.NotEqual(string.Empty, batch?.BlockId);
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

        public void Dispose()
        {
            _eventStore.Dispose();
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
