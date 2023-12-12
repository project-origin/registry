using System;
using Microsoft.Extensions.Options;

namespace ProjectOrigin.Electricity.Server.Options;

public class RegistryOptionsValidator : IValidateOptions<RegistryOptions>
{
    public ValidateOptionsResult Validate(string? name, RegistryOptions options)
    {
        foreach (var registry in options.Registries)
        {
            bool valid = Uri.TryCreate(registry.Value.Address, UriKind.Absolute, out var uriResult) &&
                 (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            if (!valid)
            {
                return ValidateOptionsResult.Fail($"Invalid URL address specified for registry ”{registry.Key}”");
            }
        }

        return ValidateOptionsResult.Success;
    }
}
