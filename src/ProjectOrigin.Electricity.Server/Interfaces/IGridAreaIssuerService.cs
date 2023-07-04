using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;

namespace ProjectOrigin.Electricity.Server.Interfaces;

public interface IGridAreaIssuerService
{
    IPublicKey? GetAreaPublicKey(string area);
}
