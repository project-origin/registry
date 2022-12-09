using NSec.Cryptography;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity;

internal abstract class AbstractCertificate
{
    public CertificateSlice? GetCertificateSlice(V1.SliceId id) => _availableSlices.GetValueOrDefault(id);
    public bool HasClaim(Register.V1.Uuid allocationId) => _claimedSlices.ContainsKey(allocationId);
    public bool HasAllocation(Register.V1.Uuid allocationId) => _allocationSlices.ContainsKey(allocationId);
    public AllocationSlice? GetAllocation(Register.V1.Uuid allocationId) => _allocationSlices.GetValueOrDefault(allocationId);

    private Dictionary<V1.SliceId, CertificateSlice> _availableSlices = new Dictionary<V1.SliceId, CertificateSlice>();
    private Dictionary<Register.V1.Uuid, AllocationSlice> _allocationSlices = new Dictionary<Register.V1.Uuid, AllocationSlice>();
    private Dictionary<Register.V1.Uuid, AllocationSlice> _claimedSlices = new Dictionary<Register.V1.Uuid, AllocationSlice>();

    public void Apply(V1.ClaimedEvent e)
    {
        var slice = GetAllocation(e.AllocationId) ?? throw new Exception("Invalid state");
        _allocationSlices.Remove(e.AllocationId);
        _claimedSlices.Add(e.AllocationId, slice);
    }

    public void Apply(V1.SlicedEvent e)
    {
        TakeAvailableSlice(e.SourceSlice);
        foreach (var newSlice in e.NewSlices)
        {
            AddAvailableSlice(newSlice.Quantity.ToModel(), newSlice.NewOwner);
        }
    }

    protected CertificateSlice TakeAvailableSlice(V1.SliceId sliceId)
    {
        var oldSlice = GetCertificateSlice(sliceId) ?? throw new Exception("Invalid state");
        _availableSlices.Remove(sliceId);
        return oldSlice;
    }

    protected void AddAvailableSlice(Commitment commitment, V1.PublicKey key)
    {
        var publicKey = PublicKey.Import(SignatureAlgorithm.Ed25519, key.Content.ToByteArray(), KeyBlobFormat.RawPublicKey);
        var slice = new CertificateSlice(commitment, publicKey);
        _availableSlices.Add(slice.Id, slice);
    }

    protected void AllocateSlice(V1.SliceId id, V1.AllocatedEvent e)
    {
        var oldSlice = TakeAvailableSlice(id);
        var newSlice = new AllocationSlice(oldSlice.Commitment, oldSlice.Owner, e.AllocationId, e.ProductionCertificateId, e.ConsumptionCertificateId);
        _allocationSlices.Add(e.AllocationId, newSlice);
    }


}
