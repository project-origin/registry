using System.Security.Cryptography;
using Google.Protobuf;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Models;

namespace ProjectOrigin.Electricity.Production;

internal class ProductionCertificate
{
    public Register.V1.FederatedStreamId Id { get => _issued.CertificateId; }
    public DateInterval Period { get => _issued.Period.ToModel(); }
    public string GridArea { get => _issued.GridArea; }

    internal CertificateSlice? GetCertificateSlice(V1.SliceId id) => _availableSlices.GetValueOrDefault(id);
    public bool HasClaim(Guid allocationId) => _claimedSlices.SingleOrDefault(x => x.AllocationId == allocationId) is not null;
    public bool HasAllocation(Guid allocationId) => _allocationSlices.SingleOrDefault(x => x.AllocationId == allocationId) is not null;
    public AllocationSlice? GetAllocation(Guid allocationId) => _allocationSlices.SingleOrDefault(x => x.AllocationId == allocationId);

    private V1.ProductionIssuedEvent _issued;
    private Dictionary<V1.SliceId, CertificateSlice> _availableSlices = new Dictionary<V1.SliceId, CertificateSlice>();
    private List<AllocationSlice> _allocationSlices = new List<AllocationSlice>();
    private List<AllocationSlice> _claimedSlices = new List<AllocationSlice>();

    internal ProductionCertificate(V1.ProductionIssuedEvent e)
    {
        _issued = e;
        var publicKey = PublicKey.Import(SignatureAlgorithm.Ed25519, e.OwnerPublicKey.Content.ToByteArray(), KeyBlobFormat.RawPublicKey);

        var slice = new CertificateSlice(e.QuantityCommitment.ToModel(), publicKey);
        _availableSlices.Add(slice.Id, slice);
    }

    public void Apply(V1.TransferredEvent e)
    {
        throw new NotImplementedException();
    }

    public void Apply(V1.SlicedEvent e)
    {
        throw new NotImplementedException();

        // var oldSlice = _availableSlices.Single(slice => slice.Commitment == e.Slice.Source.ToModel());
        // _availableSlices.Remove(oldSlice);
        // var publicKey = PublicKey.Import(SignatureAlgorithm.Ed25519, e.NewOwner.ToByteArray(), KeyBlobFormat.RawPublicKey);
        // _availableSlices.Add(new(e.Slice.Quantity.ToModel(), publicKey));
        // _availableSlices.Add(new(e.Slice.Remainder.ToModel(), oldSlice.Owner));
    }

    public void Apply(V1.AllocatedEvent e)
    {
        throw new NotImplementedException();

        // var oldSlice = _availableSlices.Single(slice => slice.Commitment == e.Slice.Source.ToModel());
        // _availableSlices.Remove(oldSlice);
        // _allocationSlices.Add(new(e.Slice.Quantity.ToModel(), oldSlice.Owner, e.AllocationId.ToModel(), e.ProductionCertificateId.ToModel(), e.ConsumptionCertificateId.ToModel()));
        // _availableSlices.Add(new(e.Slice.Remainder.ToModel(), oldSlice.Owner));
    }

    public void Apply(V1.ClaimedEvent e)
    {
        throw new NotImplementedException();

        // var allocationSlice = _allocationSlices.Single(slice => slice.AllocationId == e.AllocationId.ToModel());
        // _allocationSlices.Remove(allocationSlice);
        // _claimedSlices.Add(allocationSlice);
    }
}
