using System.Collections.Generic;

namespace ProjectOrigin.Electricity.Server.Options;

public class RegistryOptions
{
    public Dictionary<string, RegistryInfo> Registries { get; set; } = new Dictionary<string, RegistryInfo>();
}

public class RegistryInfo
{
    public string Address { get; set; } = null!;
}
