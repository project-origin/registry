using System.Numerics;
using Google.Protobuf;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Tests;

public class ConsumptionCertificateApplyTests
{
    private Fixture _fix = new Fixture();

    private Register.V1.FederatedStreamId CreateId()
    {
        var registry = _fix.Create<string>();
        var streamId = Guid.NewGuid();

        return new Register.V1.FederatedStreamId
        {
            Registry = registry,
            StreamId = new Register.V1.Uuid
            {
                Value = streamId.ToString()
            }
        };
    }

    private (ConsumptionCertificate, CommitmentParameters) Create()
    {
        var area = _fix.Create<string>();
        var period = new DateInterval(
            new DateTimeOffset(2022, 09, 25, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2022, 09, 25, 13, 0, 0, TimeSpan.Zero));
        var gsrn = Group.Default.Commit(_fix.Create<BigInteger>());
        var quantity = Group.Default.Commit(_fix.Create<BigInteger>());
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);


        var @event = new V1.ConsumptionIssuedEvent()
        {
            CertificateId = CreateId(),
            Period = period.ToProto(),
            GridArea = area,
            GsrnCommitment = gsrn.ToProtoCommitment(),
            QuantityCommitment = quantity.ToProtoCommitment(),
            OwnerPublicKey = ownerKey.PublicKey.ToProto(),
        };

        var cert = new ConsumptionCertificate(@event);

        return (cert, quantity);
    }

    [Fact]
    public void ConsumptionCertificate_Create()
    {

        var registry = _fix.Create<string>();
        var streamId = Guid.NewGuid();
        var area = _fix.Create<string>();
        var period = new DateInterval(
            new DateTimeOffset(2022, 09, 25, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2022, 09, 25, 13, 0, 0, TimeSpan.Zero));
        var gsrn = Group.Default.Commit(_fix.Create<BigInteger>());
        var quantity = Group.Default.Commit(_fix.Create<BigInteger>());
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);


        var @event = new V1.ConsumptionIssuedEvent()
        {
            CertificateId = new Register.V1.FederatedStreamId
            {
                Registry = registry,
                StreamId = new Register.V1.Uuid
                {
                    Value = streamId.ToString()
                }
            },
            Period = period.ToProto(),
            GridArea = area,
            GsrnCommitment = gsrn.ToProtoCommitment(),
            QuantityCommitment = quantity.ToProtoCommitment(),
            OwnerPublicKey = ownerKey.PublicKey.ToProto(),
        };

        var cert = new ConsumptionCertificate(@event);

        Assert.Equal(registry, cert.Id.Registry);
        Assert.Equal(streamId, cert.Id.StreamId.ToModel());
        Assert.Equal(period.Start, cert.Period.Start);
        Assert.Equal(period.End, cert.Period.End);
        Assert.NotNull(cert.GetCertificateSlice(quantity.ToSliceId()));
    }

    [Fact]
    public void ConsumptionCertificate_Apply_SliceEvent()
    {
        var allocationId = Guid.NewGuid().ToProto();
        var (cert, slice0) = Create();

        var @event = new V1.SlicedEvent()
        {
            CertificateId = cert.Id,
            SourceSlice = slice0.ToSliceId(),
        };

        var slice1 = Group.Default.Commit(_fix.Create<BigInteger>());
        var owner1 = Key.Create(SignatureAlgorithm.Ed25519);
        @event.NewSlices.Add(new V1.SlicedEvent.Types.Slice
        {
            Quantity = slice1.ToProtoCommitment(),
            NewOwner = owner1.PublicKey.ToProto()
        });

        var slice2 = Group.Default.Commit(_fix.Create<BigInteger>());
        var owner2 = Key.Create(SignatureAlgorithm.Ed25519);
        @event.NewSlices.Add(new V1.SlicedEvent.Types.Slice
        {
            Quantity = slice2.ToProtoCommitment(),
            NewOwner = owner2.PublicKey.ToProto()
        });

        cert.Apply(@event);

        Assert.Null(cert.GetCertificateSlice(slice0.ToSliceId()));
        Assert.NotNull(cert.GetCertificateSlice(slice1.ToSliceId()));
        Assert.NotNull(cert.GetCertificateSlice(slice2.ToSliceId()));
    }

    [Fact]
    public void ConsumptionCertificate_Apply_AllocatedEvent()
    {
        var allocationId = Guid.NewGuid().ToProto();
        var productionId = CreateId();
        var (cert, consQuantity) = Create();
        var prodQuantity = Group.Default.Commit(_fix.Create<BigInteger>());

        var @event = new V1.AllocatedEvent()
        {
            AllocationId = allocationId,
            ProductionCertificateId = productionId,
            ConsumptionCertificateId = cert.Id,
            ProductionSourceSlice = prodQuantity.ToSliceId(),
            ConsumptionSourceSlice = consQuantity.ToSliceId(),
            EqualityProof = ByteString.CopyFrom(Group.Default.CreateEqualityProof(consQuantity, prodQuantity))
        };

        cert.Apply(@event);

        Assert.Null(cert.GetCertificateSlice(consQuantity.ToSliceId()));
        Assert.NotNull(cert.GetAllocation(allocationId));
        Assert.False(cert.HasClaim(allocationId));
        Assert.True(cert.HasAllocation(allocationId));
    }

    [Fact]
    public void ConsumptionCertificate_Apply_ClaimedEvent()
    {
        var allocationId = Guid.NewGuid().ToProto();
        var productionId = CreateId();
        var (cert, consQuantity) = Create();
        var prodQuantity = Group.Default.Commit(_fix.Create<BigInteger>());

        var allocationEvent = new V1.AllocatedEvent()
        {
            AllocationId = allocationId,
            ProductionCertificateId = productionId,
            ConsumptionCertificateId = cert.Id,
            ProductionSourceSlice = prodQuantity.ToSliceId(),
            ConsumptionSourceSlice = consQuantity.ToSliceId(),
            EqualityProof = ByteString.CopyFrom(Group.Default.CreateEqualityProof(consQuantity, prodQuantity))
        };
        cert.Apply(allocationEvent);

        var claimedEvent = new V1.ClaimedEvent
        {
            AllocationId = allocationId,
            CertificateId = cert.Id,
        };
        cert.Apply(claimedEvent);

        Assert.Null(cert.GetCertificateSlice(consQuantity.ToSliceId()));
        Assert.Null(cert.GetAllocation(allocationId));
        Assert.False(cert.HasAllocation(allocationId));
        Assert.True(cert.HasClaim(allocationId));
    }
}
