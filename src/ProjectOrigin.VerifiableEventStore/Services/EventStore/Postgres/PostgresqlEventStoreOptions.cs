namespace ProjectOrigin.VerifiableEventStore.Services.EventStore.Postgres;

public sealed class PostgresqlEventStoreOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}
