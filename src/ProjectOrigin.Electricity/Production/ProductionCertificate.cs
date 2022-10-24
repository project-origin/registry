using NSec.Cryptography;
using ProjectOrigin.Electricity.Production.Requests;
using ProjectOrigin.Electricity.Shared.Internal;
using ProjectOrigin.RequestProcessor.Interfaces;

namespace ProjectOrigin.Electricity.Production;

internal class ProductionCertificate : IModel
{
    public FederatedStreamId Id { get => issued!.Id; }

    public IEnumerable<CertificateSlice> AvailableSlices { get => availableSlices; }
    public IEnumerable<AllocationSlice> AllocationSlices { get => allocationSlices; }

    private ProductionIssuedEvent issued;
    private List<CertificateSlice> availableSlices = new List<CertificateSlice>();
    private List<AllocationSlice> allocationSlices = new List<AllocationSlice>();
    private List<AllocationSlice> claimedSlices = new List<AllocationSlice>();

    public void Apply(ProductionIssuedEvent e)
    {
        issued = e;
        var publicKey = PublicKey.Import(SignatureAlgorithm.Ed25519, e.OwnerPublicKey, KeyBlobFormat.RawPublicKey);
        availableSlices.Add(new(e.QuantityCommitment, publicKey));
    }

    public void Apply(ProductionSliceTransferredEvent e)
    {
        var oldSlice = availableSlices.Single(slice => slice.Commitment == e.Slice.Source);
        availableSlices.Remove(oldSlice);
        var publicKey = PublicKey.Import(SignatureAlgorithm.Ed25519, e.NewOwner, KeyBlobFormat.RawPublicKey);
        availableSlices.Add(new(e.Slice.Quantity, publicKey));
        availableSlices.Add(new(e.Slice.Remainder, oldSlice.Owner));
    }

    internal bool HasClaim(Guid allocationId) => claimedSlices.SingleOrDefault(x => x.AllocationId == allocationId) is not null;

    public bool HasAllocation(Guid allocationId) => allocationSlices.SingleOrDefault(x => x.AllocationId == allocationId) is not null;

    public AllocationSlice? GetAllocation(Guid allocationId) => allocationSlices.SingleOrDefault(x => x.AllocationId == allocationId);

    public void Apply(ProductionAllocatedEvent e)
    {
        var oldSlice = availableSlices.Single(slice => slice.Commitment == e.Slice.Source);
        availableSlices.Remove(oldSlice);
        allocationSlices.Add(new(e.Slice.Quantity, oldSlice.Owner, e.AllocationId, e.ProductionCertificateId, e.ConsumptionCertificateId));
        availableSlices.Add(new(e.Slice.Remainder, oldSlice.Owner));
    }

    public void Apply(ProductionClaimedEvent e)
    {
        var allocationSlice = allocationSlices.Single(slice => slice.AllocationId == e.AllocationId);
        allocationSlices.Remove(allocationSlice);
        claimedSlices.Add(allocationSlice);
    }
}
