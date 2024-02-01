using System.ComponentModel.DataAnnotations;

namespace ProjectOrigin.Registry.Server.Options;

public record RegistryOptions()
{
    [Required(AllowEmptyStrings = false)]
    public string RegistryName { get; init; } = string.Empty;
}
