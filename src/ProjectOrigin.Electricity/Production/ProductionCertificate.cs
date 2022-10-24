using ProjectOrigin.Electricity.Production.Requests;
using ProjectOrigin.Electricity.Shared;
using ProjectOrigin.Electricity.Shared.Internal;

namespace ProjectOrigin.Electricity.Production;

internal class ProductionCertificate
{
    public FederatedStreamId Id { get => issued!.Id; }

    private ProductionIssuedEvent issued;

    public void Apply(ProductionIssuedEvent issued)
    {
        this.issued = issued;
    }
}
