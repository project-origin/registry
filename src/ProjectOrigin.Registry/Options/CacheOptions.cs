
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectOrigin.Registry.Server.Options;

public record CacheOptions() : IValidatableObject
{
    public CacheTypes Type { get; init; }

    public RedisOptions? Redis { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Type == CacheTypes.Redis)
        {
            if (Redis is null)
                yield return new ValidationResult("Redis options must be set when using Redis cache", new[] { nameof(Redis) });
            else
            {
                foreach (var result in Redis.Validate(validationContext))
                {
                    yield return result;
                }
            }
        }
    }
}
