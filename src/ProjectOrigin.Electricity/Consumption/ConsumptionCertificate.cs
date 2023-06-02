using ProjectOrigin.Electricity.Extensions;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Electricity.Models;

namespace ProjectOrigin.Electricity.Consumption;

public class ConsumptionCertificate : AbstractCertificate
{
    public Common.V1.FederatedStreamId Id { get => _issued.CertificateId; }
    public DateInterval Period { get => _issued.Period.ToModel(); }
    public string GridArea { get => _issued.GridArea; }

    private V1.ConsumptionIssuedEvent _issued;

    public ConsumptionCertificate(V1.ConsumptionIssuedEvent e, IKeyAlgorithm keyAlgorithm)
        : base(keyAlgorithm)
    {
        _issued = e;
        AddAvailableSlice(e.QuantityCommitment.ToModel(), e.OwnerPublicKey);
    }

    public void Apply(V1.AllocatedEvent e)
    {
        AllocateSlice(e.ConsumptionSourceSlice, e);
    }
}
