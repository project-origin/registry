using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

public class RegistryOptions
{
    public Dictionary<string, RegistryInfo> Registries { get; set; } = new Dictionary<string, RegistryInfo>();

    public bool Verify()
    {
        foreach (var registry in Registries)
        {
            bool valid = Uri.TryCreate(registry.Value.Address, UriKind.Absolute, out var uriResult) &&
                 (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            if (!valid)
            {
                throw new OptionsValidationException(nameof(RegistryOptions), typeof(RegistryOptions), new string[] { $"Invalid URL address specified for registry ”{registry.Key}”" });
            }
        }

        return true;
    }
}

public class RegistryInfo
{
    public string Address { get; set; } = null!;
}
