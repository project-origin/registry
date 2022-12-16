namespace ProjectOrigin.VerifiableEventStore.Services.EventStore.Postgres;

public sealed class PostgresqlEventStoreOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public bool CreateSchema { get; set; } = false;
    public int BatchExponent { get; set; } = 10;
}
