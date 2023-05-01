using System.ComponentModel.DataAnnotations;

namespace ProjectOrigin.VerifiableEventStore.Models;

public class VerifiableEventStoreOptions
{
    [Required, Range(0, 20)]
    public long BatchSizeExponent { get; set; }
}
