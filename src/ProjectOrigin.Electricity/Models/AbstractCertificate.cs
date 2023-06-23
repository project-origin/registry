using System;
using System.Collections.Generic;
using Google.Protobuf;
using ProjectOrigin.Electricity.Extensions;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity;

public abstract class AbstractCertificate
{
    public CertificateSlice? GetCertificateSlice(ByteString id) => _availableSlices.GetValueOrDefault(id);
    public bool HasClaim(Common.V1.Uuid allocationId) => _claimedSlices.ContainsKey(allocationId);
    public bool HasAllocation(Common.V1.Uuid allocationId) => _allocationSlices.ContainsKey(allocationId);
    public AllocationSlice? GetAllocation(Common.V1.Uuid allocationId) => _allocationSlices.GetValueOrDefault(allocationId);

    private Dictionary<ByteString, CertificateSlice> _availableSlices = new Dictionary<ByteString, CertificateSlice>();
    private Dictionary<Common.V1.Uuid, AllocationSlice> _allocationSlices = new Dictionary<Common.V1.Uuid, AllocationSlice>();
    private Dictionary<Common.V1.Uuid, AllocationSlice> _claimedSlices = new Dictionary<Common.V1.Uuid, AllocationSlice>();

    public void Apply(V1.ClaimedEvent e)
    {
        var slice = GetAllocation(e.AllocationId) ?? throw new Exception("Invalid state");
        _allocationSlices.Remove(e.AllocationId);
        _claimedSlices.Add(e.AllocationId, slice);
    }

    public void Apply(V1.SlicedEvent e)
    {
        TakeAvailableSlice(e.SourceSliceHash);
        foreach (var newSlice in e.NewSlices)
        {
            AddAvailableSlice(newSlice.Quantity.ToModel(), newSlice.NewOwner);
        }
    }

    protected CertificateSlice TakeAvailableSlice(ByteString sliceHash)
    {
        var oldSlice = GetCertificateSlice(sliceHash) ?? throw new Exception("Invalid state");
        _availableSlices.Remove(sliceHash);
        return oldSlice;
    }

    protected void AddAvailableSlice(Commitment commitment, V1.PublicKey publicKey)
    {
        var slice = new CertificateSlice(commitment, publicKey);
        _availableSlices.Add(slice.Hash, slice);
    }

    protected void AllocateSlice(ByteString sliceHash, V1.AllocatedEvent e)
    {
        var oldSlice = TakeAvailableSlice(sliceHash);
        var newSlice = new AllocationSlice(oldSlice.Commitment, oldSlice.Owner, e.AllocationId, e.ProductionCertificateId, e.ConsumptionCertificateId);
        _allocationSlices.Add(e.AllocationId, newSlice);
    }
}
