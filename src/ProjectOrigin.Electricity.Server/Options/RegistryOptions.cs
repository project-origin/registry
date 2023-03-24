using System.ComponentModel.DataAnnotations;
using ProjectOrigin.VerifiableEventStore.Models;

public class RegistryOptions
{
    [Required]
    public VerifiableEventStoreOptions VerifiableEventStore { get; set; }
}
