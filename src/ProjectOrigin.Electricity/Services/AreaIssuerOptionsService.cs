using System;
using Microsoft.Extensions.Options;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;

namespace ProjectOrigin.Electricity.Services;

public class AreaIssuerOptionsService : IAreaIssuerService
{
    private IHDAlgorithm _algorithm;
    private IssuerOptions _options;

    public AreaIssuerOptionsService(IHDAlgorithm algorithm, IOptions<IssuerOptions> options)
    {

        _algorithm = algorithm;
        _options = options.Value;
    }

    public IPublicKey? GetAreaPublicKey(string area)
    {
        if (_options.Issuers.TryGetValue(area, out var base64data))
        {
            return _algorithm.ImportPublicKey(Convert.FromBase64String(base64data));
        }
        return null;
    }
}
