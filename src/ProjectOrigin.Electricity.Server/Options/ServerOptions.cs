using System.ComponentModel.DataAnnotations;

public class ServerOptions
{
    [Required]
    public Dictionary<string, RegistryOptions> Registries { get; set; }

}
