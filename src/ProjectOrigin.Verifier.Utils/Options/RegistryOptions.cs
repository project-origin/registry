using System.Collections.Generic;

public class RegistryOptions
{
    public Dictionary<string, RegistryInfo> Registries { get; set; } = new Dictionary<string, RegistryInfo>();
}

public class RegistryInfo
{
    public string Address { get; set; } = null!;
}
