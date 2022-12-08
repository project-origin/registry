using NSec.Cryptography;
using ProjectOrigin.Electricity.Models;

namespace ProjectOrigin.Electricity.Consumption;

internal class ConsumptionCertificate
{
    public Register.V1.FederatedStreamId Id { get => _issued.CertificateId; }
    public DateInterval Period { get => _issued.Period.ToModel(); }
    public string GridArea { get => _issued.GridArea; }

    internal CertificateSlice? GetCertificateSlice(V1.SliceId id) => _availableSlices.GetValueOrDefault(id);
    public bool HasClaim(Register.V1.Uuid allocationId) => _claimedSlices.ContainsKey(allocationId);
    public bool HasAllocation(Register.V1.Uuid allocationId) => _allocationSlices.ContainsKey(allocationId);
    public AllocationSlice? GetAllocation(Register.V1.Uuid allocationId) => _allocationSlices.GetValueOrDefault(allocationId);

    private V1.ConsumptionIssuedEvent _issued;
    private Dictionary<V1.SliceId, CertificateSlice> _availableSlices = new Dictionary<V1.SliceId, CertificateSlice>();
    private Dictionary<Register.V1.Uuid, AllocationSlice> _allocationSlices = new Dictionary<Register.V1.Uuid, AllocationSlice>();
    private Dictionary<Register.V1.Uuid, AllocationSlice> _claimedSlices = new Dictionary<Register.V1.Uuid, AllocationSlice>();

    public ConsumptionCertificate(V1.ConsumptionIssuedEvent e)
    {
        _issued = e;
        var publicKey = PublicKey.Import(SignatureAlgorithm.Ed25519, e.OwnerPublicKey.Content.ToByteArray(), KeyBlobFormat.RawPublicKey);


        var slice = new CertificateSlice(e.QuantityCommitment.ToModel(), publicKey);
        _availableSlices.Add(slice.Id, slice);
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
        var oldSlice = GetCertificateSlice(e.ProductionSourceSlice) ?? throw new Exception("Invalid state");
        var newSlice = new AllocationSlice(oldSlice.Commitment, oldSlice.Owner, e.AllocationId, e.ProductionCertificateId, e.ConsumptionCertificateId);
        _availableSlices.Remove(e.ProductionSourceSlice);
        _allocationSlices.Add(e.AllocationId, newSlice);
    }

    public void Apply(V1.ClaimedEvent e)
    {
        var slice = GetAllocation(e.AllocationId) ?? throw new Exception("Invalid state");
        _allocationSlices.Remove(e.AllocationId);
        _claimedSlices.Add(e.AllocationId, slice);
    }
}
