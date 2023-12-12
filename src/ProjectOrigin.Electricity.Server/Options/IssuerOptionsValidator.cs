using System;
using System.Text;
using Microsoft.Extensions.Options;
using ProjectOrigin.HierarchicalDeterministicKeys;

namespace ProjectOrigin.Electricity.Server.Options;

public class IssuerOptionsValidator : IValidateOptions<IssuerOptions>
{
    public ValidateOptionsResult Validate(string? name, IssuerOptions options)
    {
        if (options.Issuers.Count == 0)
            return ValidateOptionsResult.Fail("No Issuer areas configured.");

        foreach (var pair in options.Issuers)
        {
            try
            {
                var keyText = Encoding.UTF8.GetString(Convert.FromBase64String(pair.Value));
                Algorithms.Ed25519.ImportPublicKeyText(keyText);
            }
            catch (Exception)
            {
                return ValidateOptionsResult.Fail($"A issuer key ”{pair.Key}” is a invalid format.");
            }
        }

        return ValidateOptionsResult.Success;
    }
}
