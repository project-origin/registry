using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Google.Protobuf;

namespace ProjectOrigin.Electricity.Models;

public class GranularCertificate
{
    public Common.V1.FederatedStreamId Id => _issued.CertificateId;
    public V1.GranularCertificateType Type => _issued.Type;
    public V1.DateInterval Period => _issued.Period;
    public string GridArea => _issued.GridArea;

    private readonly V1.IssuedEvent _issued;
    private readonly Dictionary<ByteString, CertificateSlice> _availableSlices = new Dictionary<ByteString, CertificateSlice>();
    private readonly Dictionary<Common.V1.Uuid, AllocationSlice> _allocationSlices = new Dictionary<Common.V1.Uuid, AllocationSlice>();
    private readonly Dictionary<Common.V1.Uuid, AllocationSlice> _claimedSlices = new Dictionary<Common.V1.Uuid, AllocationSlice>();

    public GranularCertificate(V1.IssuedEvent e)
    {
        _issued = e;
        AddAvailableSlice(e.QuantityCommitment, e.OwnerPublicKey);
    }

    public void Apply(V1.TransferredEvent e)
    {
        var oldSlice = TakeAvailableSlice(e.SourceSliceHash);
        AddAvailableSlice(oldSlice.Commitment, e.NewOwner);
    }

    public void Apply(V1.AllocatedEvent e)
    {
        if (_issued.Type == V1.GranularCertificateType.Production)
            AllocateSlice(e.ProductionSourceSliceHash, e);
        else if (_issued.Type == V1.GranularCertificateType.Consumption)
            AllocateSlice(e.ConsumptionSourceSliceHash, e);
        else
            throw new NotSupportedException($"Certificate type ”{_issued.Type.ToString()}” is not supported");
    }

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
            AddAvailableSlice(newSlice.Quantity, newSlice.NewOwner);
        }
    }

    public CertificateSlice? GetCertificateSlice(ByteString id) => _availableSlices.GetValueOrDefault(id);
    public bool HasClaim(Common.V1.Uuid allocationId) => _claimedSlices.ContainsKey(allocationId);
    public bool HasAllocation(Common.V1.Uuid allocationId) => _allocationSlices.ContainsKey(allocationId);
    public AllocationSlice? GetAllocation(Common.V1.Uuid allocationId) => _allocationSlices.GetValueOrDefault(allocationId);

    protected CertificateSlice TakeAvailableSlice(ByteString sliceHash)
    {
        var oldSlice = GetCertificateSlice(sliceHash) ?? throw new Exception("Invalid state");
        _availableSlices.Remove(sliceHash);
        return oldSlice;
    }

    protected void AddAvailableSlice(V1.Commitment commitment, V1.PublicKey publicKey)
    {
        var slice = new CertificateSlice(commitment, publicKey);
        var sliceHash = ByteString.CopyFrom(SHA256.HashData(commitment.Content.ToByteArray()));
        _availableSlices.Add(sliceHash, slice);
    }

    protected void AllocateSlice(ByteString sliceHash, V1.AllocatedEvent e)
    {
        var oldSlice = TakeAvailableSlice(sliceHash);
        var newSlice = new AllocationSlice(oldSlice.Commitment, oldSlice.Owner, e.AllocationId, e.ProductionCertificateId, e.ConsumptionCertificateId);
        _allocationSlices.Add(e.AllocationId, newSlice);
    }
}
