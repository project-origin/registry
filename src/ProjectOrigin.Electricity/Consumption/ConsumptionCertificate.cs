using NSec.Cryptography;
using ProjectOrigin.Electricity.Consumption.Requests;
using ProjectOrigin.Electricity.Shared.Internal;
using ProjectOrigin.RequestProcessor.Interfaces;

namespace ProjectOrigin.Electricity.Consumption;

internal class ConsumptionCertificate : IModel
{
    public FederatedStreamId Id { get => issued.CertificateId; }
    public IEnumerable<CertificateSlice> AvailableSlices { get => availableSlices; }
    public IEnumerable<AllocationSlice> AllocationSlices { get => allocationSlices; }

    private List<CertificateSlice> availableSlices = new List<CertificateSlice>();
    private List<AllocationSlice> allocationSlices = new List<AllocationSlice>();
    private List<AllocationSlice> claimedSlices = new List<AllocationSlice>();
    private ConsumptionIssuedEvent issued;

    public bool HasAllocation(Guid allocationId)
    {
        return allocationSlices.SingleOrDefault(a => a.AllocationId == allocationId) is not null;
    }

    public void Apply(ConsumptionIssuedEvent e)
    {
        this.issued = e;
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
