using System.Security.Cryptography;
using Google.Protobuf;
using ProjectOrigin.Electricity.Extensions;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.PedersenCommitment;
using Xunit;
using System;
using AutoFixture;
using ProjectOrigin.HierarchicalDeterministicKeys;
using Google.Protobuf.WellKnownTypes;

namespace ProjectOrigin.Electricity.Tests;

public class ConsumptionCertificateApplyTests
{
    private Fixture _fixture = new Fixture();

    private Common.V1.FederatedStreamId CreateId()
    {
        var registry = _fixture.Create<string>();
        var streamId = Guid.NewGuid();

        return new Common.V1.FederatedStreamId
        {
            Registry = registry,
            StreamId = new Common.V1.Uuid
            {
                Value = streamId.ToString()
            }
        };
    }

    private (GranularCertificate, SecretCommitmentInfo) Create()
    {
        var area = _fixture.Create<string>();
        var period = new V1.DateInterval
        {
            Start = Timestamp.FromDateTimeOffset(new DateTimeOffset(2022, 09, 25, 12, 0, 0, TimeSpan.Zero)),
            End = Timestamp.FromDateTimeOffset(new DateTimeOffset(2022, 09, 25, 13, 0, 0, TimeSpan.Zero))
        };
        var gsrnHash = SHA256.HashData(BitConverter.GetBytes(new Fixture().Create<ulong>()));
        var quantity = new SecretCommitmentInfo(_fixture.Create<uint>());
        var ownerKey = Algorithms.Secp256k1.GenerateNewPrivateKey();
        var certId = CreateId();

        var @event = new V1.IssuedEvent()
        {
            CertificateId = certId,
            Type = V1.GranularCertificateType.Consumption,
            Period = period,
            GridArea = area,
            AssetIdHash = ByteString.CopyFrom(gsrnHash),
            QuantityCommitment = quantity.ToProtoCommitment(certId.StreamId.Value),
            OwnerPublicKey = ownerKey.PublicKey.ToProto(),
        };

        var cert = new GranularCertificate(@event);

        return (cert, quantity);
    }

    [Fact]
    public void ConsumptionCertificate_Create()
    {

        var registry = _fixture.Create<string>();
        var streamId = Guid.NewGuid();
        var area = _fixture.Create<string>();
        var period = new V1.DateInterval
        {
            Start = Timestamp.FromDateTimeOffset(new DateTimeOffset(2022, 09, 25, 12, 0, 0, TimeSpan.Zero)),
            End = Timestamp.FromDateTimeOffset(new DateTimeOffset(2022, 09, 25, 13, 0, 0, TimeSpan.Zero))
        };
        var gsrnHash = SHA256.HashData(BitConverter.GetBytes(_fixture.Create<ulong>()));
        var quantity = new SecretCommitmentInfo(_fixture.Create<uint>());
        var ownerKey = Algorithms.Secp256k1.GenerateNewPrivateKey();


        var @event = new V1.IssuedEvent()
        {
            CertificateId = new Common.V1.FederatedStreamId
            {
                Registry = registry,
                StreamId = new Common.V1.Uuid
                {
                    Value = streamId.ToString()
                }
            },
            Type = V1.GranularCertificateType.Consumption,
            Period = period,
            GridArea = area,
            AssetIdHash = ByteString.CopyFrom(gsrnHash),
            QuantityCommitment = quantity.ToProtoCommitment(streamId.ToString()),
            OwnerPublicKey = ownerKey.PublicKey.ToProto(),
        };

        var cert = new GranularCertificate(@event);

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
            SourceSliceHash = slice0.ToSliceId(),
        };

        var slice1 = new SecretCommitmentInfo(_fixture.Create<uint>());
        var owner1 = Algorithms.Secp256k1.GenerateNewPrivateKey();
        @event.NewSlices.Add(new V1.SlicedEvent.Types.Slice
        {
            Quantity = slice1.ToProtoCommitment(cert.Id.StreamId.Value),
            NewOwner = owner1.PublicKey.ToProto()
        });

        var slice2 = new SecretCommitmentInfo(_fixture.Create<uint>());
        var owner2 = Algorithms.Secp256k1.GenerateNewPrivateKey();
        @event.NewSlices.Add(new V1.SlicedEvent.Types.Slice
        {
            Quantity = slice2.ToProtoCommitment(cert.Id.StreamId.Value),
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
        var prodQuantity = new SecretCommitmentInfo(_fixture.Create<uint>());

        var @event = new V1.AllocatedEvent()
        {
            AllocationId = allocationId,
            ProductionCertificateId = productionId,
            ConsumptionCertificateId = cert.Id,
            ProductionSourceSliceHash = prodQuantity.ToSliceId(),
            ConsumptionSourceSliceHash = consQuantity.ToSliceId(),
            EqualityProof = ByteString.CopyFrom(SecretCommitmentInfo.CreateEqualityProof(consQuantity, prodQuantity, allocationId.Value))
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
        var prodQuantity = new SecretCommitmentInfo(_fixture.Create<uint>());

        var allocationEvent = new V1.AllocatedEvent()
        {
            AllocationId = allocationId,
            ProductionCertificateId = productionId,
            ConsumptionCertificateId = cert.Id,
            ProductionSourceSliceHash = prodQuantity.ToSliceId(),
            ConsumptionSourceSliceHash = consQuantity.ToSliceId(),
            EqualityProof = ByteString.CopyFrom(SecretCommitmentInfo.CreateEqualityProof(consQuantity, prodQuantity, allocationId.Value))
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
