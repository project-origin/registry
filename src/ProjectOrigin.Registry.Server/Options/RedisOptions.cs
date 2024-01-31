using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectOrigin.Registry.Server.Options;

public record RedisOptions() : IValidatableObject
{
    public string? Password { get; init; }
    public string ConnectionString { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            yield return new ValidationResult("Redis connection string must be set", new[] { nameof(ConnectionString) });
        }
    }
}
