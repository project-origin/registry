using System.ComponentModel.DataAnnotations;

namespace ProjectOrigin.VerifiableEventStore.Services.Batcher;

public record BatcherOptions
{
    [Required, Range(0, 20)]
    public long BatchSizeExponent { get; set; }
}
