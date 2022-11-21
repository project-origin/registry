using NSec.Cryptography;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Register.LineProcessor.Interfaces;
using ProjectOrigin.Register.LineProcessor.Models;

namespace ProjectOrigin.Electricity.Consumption;

internal class ConsumptionCertificate : IModel
{
    public FederatedStreamId Id { get => issued.CertificateId.ToModel(); }
    public TimePeriod Period { get => issued.Period.ToModel(); }
    public string GridArea { get => issued.GridArea; }

    public CertificateSlice? GetCertificateSlice(Slice slice) => availableSlices.SingleOrDefault(x => x.Commitment == slice.Source);
    public bool HasClaim(Guid allocationId) => claimedSlices.SingleOrDefault(x => x.AllocationId == allocationId) is not null;
    public bool HasAllocation(Guid allocationId) => allocationSlices.SingleOrDefault(a => a.AllocationId == allocationId) is not null;
    public AllocationSlice? GetAllocation(Guid allocationId) => allocationSlices.SingleOrDefault(x => x.AllocationId == allocationId);

    private V1.IssueConsumptionCommand.Types.ConsumptionIssuedEvent issued;
    private List<CertificateSlice> availableSlices = new List<CertificateSlice>();
    private List<AllocationSlice> allocationSlices = new List<AllocationSlice>();
    private List<AllocationSlice> claimedSlices = new List<AllocationSlice>();

    public void Apply(V1.IssueConsumptionCommand.Types.ConsumptionIssuedEvent e)
    {
        issued = e;
        var publicKey = PublicKey.Import(SignatureAlgorithm.Ed25519, e.OwnerPublicKey.Content.ToByteArray(), KeyBlobFormat.RawPublicKey);
        availableSlices.Add(new(e.QuantityCommitment.ToModel(), publicKey));
    }

    public void Apply(V1.ClaimCommand.Types.AllocatedEvent e)
    {
        var oldSlice = availableSlices.Single(slice => slice.Commitment == e.Slice.Source.ToModel());
        availableSlices.Remove(oldSlice);
        allocationSlices.Add(new(e.Slice.Quantity.ToModel(), oldSlice.Owner, e.AllocationId.ToModel(), e.ProductionCertificateId.ToModel(), e.ConsumptionCertificateId.ToModel()));
        availableSlices.Add(new(e.Slice.Remainder.ToModel(), oldSlice.Owner));
    }

    public void Apply(V1.ClaimCommand.Types.ClaimedEvent e)
    {
        var allocationSlice = allocationSlices.Single(slice => slice.AllocationId == e.AllocationId.ToModel());
        allocationSlices.Remove(allocationSlice);
        claimedSlices.Add(allocationSlice);
    }
}
