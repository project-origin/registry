using System.ComponentModel.DataAnnotations;

namespace ProjectOrigin.Registry.Options;

public record RegistryOptions()
{
    [Required(AllowEmptyStrings = false)]
    public string RegistryName { get; init; } = string.Empty;

    public bool ReturnComittedForFinalized = false;
}
