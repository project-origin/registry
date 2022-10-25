using NSec.Cryptography;
using ProjectOrigin.Electricity.Consumption.Requests;
using ProjectOrigin.Electricity.Shared;
using ProjectOrigin.Electricity.Shared.Internal;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.RequestProcessor.Interfaces;

namespace ProjectOrigin.Electricity.Consumption;

internal class ConsumptionCertificate : IModel
{
    public FederatedStreamId Id { get => issued.CertificateId; }
    public TimePeriod Period { get => issued.Period; }
    public string GridArea { get => issued.GridArea; }

    public CertificateSlice? GetSlice(Commitment source) => availableSlices.SingleOrDefault(x => x.Commitment == source);
    public bool HasClaim(Guid allocationId) => claimedSlices.SingleOrDefault(x => x.AllocationId == allocationId) is not null;
    public bool HasAllocation(Guid allocationId) => allocationSlices.SingleOrDefault(a => a.AllocationId == allocationId) is not null;
    public AllocationSlice? GetAllocation(Guid allocationId) => allocationSlices.SingleOrDefault(x => x.AllocationId == allocationId);

    private ConsumptionIssuedEvent issued;
    private List<CertificateSlice> availableSlices = new List<CertificateSlice>();
    private List<AllocationSlice> allocationSlices = new List<AllocationSlice>();
    private List<AllocationSlice> claimedSlices = new List<AllocationSlice>();

    public void Apply(ConsumptionIssuedEvent e)
    {
        issued = e;
        var publicKey = PublicKey.Import(SignatureAlgorithm.Ed25519, e.OwnerPublicKey, KeyBlobFormat.RawPublicKey);
        availableSlices.Add(new(e.QuantityCommitment, publicKey));
    }

    public void Apply(ConsumptionAllocatedEvent e)
    {
        var oldSlice = availableSlices.Single(slice => slice.Commitment == e.Slice.Source);
        availableSlices.Remove(oldSlice);
        allocationSlices.Add(new(e.Slice.Quantity, oldSlice.Owner, e.AllocationId, e.ProductionCertificateId, e.ConsumptionCertificateId));
        availableSlices.Add(new(e.Slice.Remainder, oldSlice.Owner));
    }

    public void Apply(ConsumptionClaimedEvent e)
    {
        var allocationSlice = allocationSlices.Single(slice => slice.AllocationId == e.AllocationId);
        allocationSlices.Remove(allocationSlice);
        claimedSlices.Add(allocationSlice);
    }
}
