using NSec.Cryptography;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Electricity.Production;

public class ProductionCertificate : IModel
{
    public FederatedStreamId Id { get => _issued.CertificateId.ToModel(); }
    public TimePeriod Period { get => _issued.Period.ToModel(); }
    public string GridArea { get => _issued.GridArea; }

    internal CertificateSlice? GetCertificateSlice(Slice slice) => _availableSlices.SingleOrDefault(x => x.Commitment == slice.Source);
    public bool HasClaim(Guid allocationId) => _claimedSlices.SingleOrDefault(x => x.AllocationId == allocationId) is not null;
    public bool HasAllocation(Guid allocationId) => _allocationSlices.SingleOrDefault(x => x.AllocationId == allocationId) is not null;
    public AllocationSlice? GetAllocation(Guid allocationId) => _allocationSlices.SingleOrDefault(x => x.AllocationId == allocationId);

    private V1.IssueProductionCommand.Types.ProductionIssuedEvent _issued;
    private List<CertificateSlice> _availableSlices = new List<CertificateSlice>();
    private List<AllocationSlice> _allocationSlices = new List<AllocationSlice>();
    private List<AllocationSlice> _claimedSlices = new List<AllocationSlice>();

    public void Apply(V1.IssueProductionCommand.Types.ProductionIssuedEvent e)
    {
        _issued = e;
        var publicKey = PublicKey.Import(SignatureAlgorithm.Ed25519, e.OwnerPublicKey.Content.ToByteArray(), KeyBlobFormat.RawPublicKey);
        _availableSlices.Add(new(e.QuantityCommitment.ToModel(), publicKey));
    }

    public void Apply(V1.TransferProductionSliceCommand.Types.ProductionSliceTransferredEvent e)
    {
        var oldSlice = _availableSlices.Single(slice => slice.Commitment == e.Slice.Source.ToModel());
        _availableSlices.Remove(oldSlice);
        var publicKey = PublicKey.Import(SignatureAlgorithm.Ed25519, e.NewOwner.ToByteArray(), KeyBlobFormat.RawPublicKey);
        _availableSlices.Add(new(e.Slice.Quantity.ToModel(), publicKey));
        _availableSlices.Add(new(e.Slice.Remainder.ToModel(), oldSlice.Owner));
    }

    public void Apply(V1.ClaimCommand.Types.AllocatedEvent e)
    {
        var oldSlice = _availableSlices.Single(slice => slice.Commitment == e.Slice.Source.ToModel());
        _availableSlices.Remove(oldSlice);
        _allocationSlices.Add(new(e.Slice.Quantity.ToModel(), oldSlice.Owner, e.AllocationId.ToModel(), e.ProductionCertificateId.ToModel(), e.ConsumptionCertificateId.ToModel()));
        _availableSlices.Add(new(e.Slice.Remainder.ToModel(), oldSlice.Owner));
    }

    public void Apply(V1.ClaimCommand.Types.ClaimedEvent e)
    {
        var allocationSlice = _allocationSlices.Single(slice => slice.AllocationId == e.AllocationId.ToModel());
        _allocationSlices.Remove(allocationSlice);
        _claimedSlices.Add(allocationSlice);
    }
}
