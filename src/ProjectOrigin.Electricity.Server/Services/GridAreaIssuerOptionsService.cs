using System;
using System.Text;
using Microsoft.Extensions.Options;
using ProjectOrigin.Electricity.Server.Interfaces;
using ProjectOrigin.Electricity.Server.Options;
using ProjectOrigin.HierarchicalDeterministicKeys;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;

namespace ProjectOrigin.Electricity.Server.Services;

public class GridAreaIssuerOptionsService : IGridAreaIssuerService
{
    private readonly IssuerOptions _options;

    public GridAreaIssuerOptionsService(IOptions<IssuerOptions> options)
    {
        _options = options.Value;
    }

    public IPublicKey? GetAreaPublicKey(string area)
    {
        if (_options.Issuers.TryGetValue(area, out var base64))
        {
            var keyText = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            return Algorithms.Ed25519.ImportPublicKeyText(keyText);
        }
        return null;
    }
}
