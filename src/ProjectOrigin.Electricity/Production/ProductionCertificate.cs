using NSec.Cryptography;
using ProjectOrigin.Electricity.Shared;
using ProjectOrigin.Electricity.Shared.Internal;
using ProjectOrigin.Register.LineProcessor.Interfaces;
using ProjectOrigin.Register.LineProcessor.Models;

namespace ProjectOrigin.Electricity.Production;

internal class ProductionCertificate : IModel
{
    public FederatedStreamId Id { get => issued.CertificateId; }
    public TimePeriod Period { get => issued.Period; }
    public string GridArea { get => issued.GridArea; }

    internal CertificateSlice? GetCertificateSlice(Slice slice) => availableSlices.SingleOrDefault(x => x.Commitment == slice.Source);
    public bool HasClaim(Guid allocationId) => claimedSlices.SingleOrDefault(x => x.AllocationId == allocationId) is not null;
    public bool HasAllocation(Guid allocationId) => allocationSlices.SingleOrDefault(x => x.AllocationId == allocationId) is not null;
    public AllocationSlice? GetAllocation(Guid allocationId) => allocationSlices.SingleOrDefault(x => x.AllocationId == allocationId);

    private V1.IssueProductionCommand.Types.ProductionIssuedEvent issued;
    private List<CertificateSlice> availableSlices = new List<CertificateSlice>();
    private List<AllocationSlice> allocationSlices = new List<AllocationSlice>();
    private List<AllocationSlice> claimedSlices = new List<AllocationSlice>();

    public void Apply(V1.IssueProductionCommand.Types.ProductionIssuedEvent e)
    {
        issued = e;
        var publicKey = PublicKey.Import(SignatureAlgorithm.Ed25519, e.OwnerPublicKey.Content.ToByteArray(), KeyBlobFormat.RawPublicKey);
        availableSlices.Add(new(Mapper.ToModel(e.QuantityCommitment), publicKey));
    }

    public void Apply(V1.TransferProductionSliceCommand.Types.ProductionSliceTransferredEvent e)
    {
        var oldSlice = availableSlices.Single(slice => slice.Commitment == Mapper.ToModel(e.Slice.Source));
        availableSlices.Remove(oldSlice);
        var publicKey = PublicKey.Import(SignatureAlgorithm.Ed25519, e.NewOwner.ToByteArray(), KeyBlobFormat.RawPublicKey);
        availableSlices.Add(new(Mapper.ToModel(e.Slice.Quantity), publicKey));
        availableSlices.Add(new(Mapper.ToModel(e.Slice.Remainder), oldSlice.Owner));
    }

    public void Apply(V1.ClaimCommand.Types.AllocatedEvent e)
    {
        var oldSlice = availableSlices.Single(slice => slice.Commitment == Mapper.ToModel(e.Slice.Source));
        availableSlices.Remove(oldSlice);
        allocationSlices.Add(new(Mapper.ToModel(e.Slice.Quantity), oldSlice.Owner, e.AllocationId.ToGuid(), e.ProductionCertificateId, e.ConsumptionCertificateId));
        availableSlices.Add(new(Mapper.ToModel(e.Slice.Remainder), oldSlice.Owner));
    }

    public void Apply(V1.ClaimCommand.Types.ClaimedEvent e)
    {
        var allocationSlice = allocationSlices.Single(slice => slice.AllocationId == e.AllocationId.ToGuid());
        allocationSlices.Remove(allocationSlice);
        claimedSlices.Add(allocationSlice);
    }
}
