using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Options;
using ProjectOrigin.HierarchicalDeterministicKeys;

namespace ProjectOrigin.Electricity.Server.Options;

public class IssuerOptions
{
    public Dictionary<string, string> Issuers { get; set; } = new Dictionary<string, string>();

    public bool Verify()
    {
        if (Issuers.Count == 0)
            throw new OptionsValidationException(nameof(IssuerOptions), typeof(IssuerOptions), new string[] { "No Issuer areas configured." });

        foreach (var pair in Issuers)
        {
            try
            {
                var keyText = Encoding.UTF8.GetString(Convert.FromBase64String(pair.Value));
                Algorithms.Ed25519.ImportPublicKeyText(keyText);
            }
            catch (Exception)
            {
                throw new OptionsValidationException(nameof(IssuerOptions), typeof(IssuerOptions), new string[] { $"A issuer key ”{pair.Key}” is a invalid format." });
            }
        }

        return true;
    }
}
