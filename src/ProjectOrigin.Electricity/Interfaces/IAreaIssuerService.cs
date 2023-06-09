using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;

namespace ProjectOrigin.Electricity.Interfaces;

public interface IAreaIssuerService
{
    IPublicKey? GetAreaPublicKey(string area);
}
