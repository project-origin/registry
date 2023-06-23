// using ProjectOrigin.VerifiableEventStore.Services.EventStore.Postgres;

// namespace ProjectOrigin.VerifiableEventStore.Tests;

// public class PostgresqlEventStoreTests : AbstractEventStoreTests<PostgresqlEventStore>, IClassFixture<DatabaseFixture>
// {
//     private PostgresqlEventStore _eventStore;

//     public PostgresqlEventStoreTests(DatabaseFixture fixture)
//     {
//         var storeOptions = new PostgresqlEventStoreOptions
//         {
//             ConnectionString = fixture.Database.ConnectionString,
//             CreateSchema = true,
//             BatchExponent = BatchExponent
//         };
//         _eventStore = new PostgresqlEventStore(storeOptions);
//     }

//     protected override PostgresqlEventStore EventStore => _eventStore;
// }
