using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using SimpleBase;

namespace ProjectOrigin.Electricity.Models;

public class IssuerOptions
{
    public Dictionary<string, string> Issuers { get; set; } = new Dictionary<string, string>();

    public bool Verify(IHDAlgorithm algorithm)
    {
        if (Issuers.Count == 0)
            throw new OptionsValidationException(nameof(IssuerOptions), typeof(IssuerOptions), new string[] { "No Issuer areas configured." });

        foreach (var pair in Issuers)
        {
            try
            {
                algorithm.ImportPublicKey(Base58.Bitcoin.Decode(pair.Value));
            }
            catch (Exception)
            {
                throw new OptionsValidationException(nameof(IssuerOptions), typeof(IssuerOptions), new string[] { $"A issuer key ”{pair.Key}” is a invalid format." });
            }
        }

        return true;
    }
}
