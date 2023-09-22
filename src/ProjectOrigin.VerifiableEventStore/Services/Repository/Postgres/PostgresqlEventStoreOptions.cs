using System.ComponentModel.DataAnnotations;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore.Postgres;

public sealed class PostgresqlEventStoreOptions
{
    [Required]
    public required string ConnectionString { get; set; }
}
