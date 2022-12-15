using ProjectOrigin.Electricity.Models;

namespace ProjectOrigin.Electricity.Production;

internal class ProductionCertificate : AbstractCertificate
{
    public Register.V1.FederatedStreamId Id { get => _issued.CertificateId; }
    public DateInterval Period { get => _issued.Period.ToModel(); }
    public string GridArea { get => _issued.GridArea; }

    private V1.ProductionIssuedEvent _issued;

    internal ProductionCertificate(V1.ProductionIssuedEvent e)
    {
        _issued = e;
        AddAvailableSlice(e.QuantityCommitment.ToModel(), e.OwnerPublicKey);
    }

    public void Apply(V1.TransferredEvent e)
    {
        var oldSlice = TakeAvailableSlice(e.SourceSlice);
        AddAvailableSlice(oldSlice.Commitment, e.NewOwner);
    }

    public void Apply(V1.AllocatedEvent e)
    {
        AllocateSlice(e.ProductionSourceSlice, e);
    }
}
