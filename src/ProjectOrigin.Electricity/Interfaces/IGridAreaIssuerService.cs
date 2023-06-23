using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;

namespace ProjectOrigin.Electricity.Interfaces;

public interface IGridAreaIssuerService
{
    IPublicKey? GetAreaPublicKey(string area);
}
