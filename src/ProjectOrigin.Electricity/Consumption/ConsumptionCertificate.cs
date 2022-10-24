using ProjectOrigin.Electricity.Consumption.Requests;
using ProjectOrigin.Electricity.Shared.Internal;

namespace ProjectOrigin.Electricity.Consumption;

internal class ConsumptionCertificate
{
    public IEnumerable<CertificateSlices> Slices { get => slices; }

    private List<CertificateSlices> slices = new List<CertificateSlices>();

    public void Apply(ConsumptionIssuedEvent issued)
    {

    }

    public void Apply(ConsumptionAllocatedEvent issued)
    {

    }

    public void Apply(ConsumptionClaimedEvent issued)
    {

    }

}
