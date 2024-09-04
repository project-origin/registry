using System.ComponentModel.DataAnnotations;

namespace ProjectOrigin.Registry.Repository.Postgres;

public sealed class PostgresqlEventStoreOptions
{
    [Required]
    public required string ConnectionString { get; set; }
}
