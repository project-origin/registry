using System;
using System.Security.Cryptography;
using AutoFixture;
using Google.Protobuf;
using ProjectOrigin.Electricity.Extensions;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Electricity.Production;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.WalletSystem.Server.HDWallet;
using Xunit;

namespace ProjectOrigin.Electricity.Tests;

public class ProductionCertificateApplyTests
{
    private Fixture _fix = new Fixture();
    private IKeyAlgorithm _algorithm = new Secp256k1Algorithm();

    private Common.V1.FederatedStreamId CreateId()
    {
        var registry = _fix.Create<string>();
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

    private (ProductionCertificate, SecretCommitmentInfo) Create()
    {
        var area = _fix.Create<string>();
        var period = new DateInterval(
            new DateTimeOffset(2022, 09, 25, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2022, 09, 25, 13, 0, 0, TimeSpan.Zero));
        var gsrnHash = SHA256.HashData(BitConverter.GetBytes(new Fixture().Create<ulong>()));
        var quantity = new SecretCommitmentInfo(_fix.Create<uint>());
        var ownerKey = _algorithm.Create();
        var certId = CreateId();

        var @event = new V1.ProductionIssuedEvent()
        {
            CertificateId = certId,
            Period = period.ToProto(),
            GridArea = area,
            GsrnHash = ByteString.CopyFrom(gsrnHash),
            QuantityCommitment = quantity.ToProtoCommitment(certId.StreamId.Value),
            OwnerPublicKey = ownerKey.PublicKey.ToProto(),
        };

        var cert = new ProductionCertificate(@event, _algorithm);

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
        var gsrnHash = SHA256.HashData(BitConverter.GetBytes(new Fixture().Create<ulong>()));
        var quantity = new SecretCommitmentInfo(_fix.Create<uint>());
        var ownerKey = _algorithm.Create();


        var @event = new V1.ProductionIssuedEvent()
        {
            CertificateId = new Common.V1.FederatedStreamId
            {
                Registry = registry,
                StreamId = new Common.V1.Uuid
                {
                    Value = streamId.ToString()
                }
            },
            Period = period.ToProto(),
            GridArea = area,
            GsrnHash = ByteString.CopyFrom(gsrnHash),
            QuantityCommitment = quantity.ToProtoCommitment(streamId.ToString()),
            OwnerPublicKey = ownerKey.PublicKey.ToProto(),
        };

        var cert = new ProductionCertificate(@event, _algorithm);

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

        var slice1 = new SecretCommitmentInfo(_fix.Create<uint>());
        var owner1 = _algorithm.Create();
        @event.NewSlices.Add(new V1.SlicedEvent.Types.Slice
        {
            Quantity = slice1.ToProtoCommitment(cert.Id.StreamId.Value),
            NewOwner = owner1.PublicKey.ToProto()
        });

        var slice2 = new SecretCommitmentInfo(_fix.Create<uint>());
        var owner2 = _algorithm.Create();
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
    public void ConsumptionCertificate_Apply_TransferEvent()
    {
        var allocationId = Guid.NewGuid().ToProto();
        var (cert, slice0) = Create();

        var newOwner = _algorithm.Create();

        var @event = new V1.TransferredEvent()
        {
            CertificateId = cert.Id,
            SourceSlice = slice0.ToSliceId(),
            NewOwner = newOwner.PublicKey.ToProto()
        };

        var slice = cert.GetCertificateSlice(slice0.ToSliceId());
        Assert.NotNull(slice);
        Assert.NotEqual(newOwner.PublicKey, slice!.Owner);

        cert.Apply(@event);
        slice = cert.GetCertificateSlice(slice0.ToSliceId());
        Assert.NotNull(slice);
        Assert.Equal(newOwner.PublicKey, slice!.Owner);
    }

    [Fact]
    public void ConsumptionCertificate_Apply_AllocatedEvent()
    {
        var allocationId = Guid.NewGuid().ToProto();
        var consumptionId = CreateId();
        var (cert, prodQuantity) = Create();
        var consQuantity = new SecretCommitmentInfo(_fix.Create<uint>());

        var @event = new V1.AllocatedEvent()
        {
            AllocationId = allocationId,
            ProductionCertificateId = cert.Id,
            ConsumptionCertificateId = consumptionId,
            ProductionSourceSlice = prodQuantity.ToSliceId(),
            ConsumptionSourceSlice = consQuantity.ToSliceId(),
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
        var consumptionId = CreateId();
        var (cert, prodQuantity) = Create();
        var consQuantity = new SecretCommitmentInfo(_fix.Create<uint>());

        var allocationEvent = new V1.AllocatedEvent()
        {
            AllocationId = allocationId,
            ProductionCertificateId = cert.Id,
            ConsumptionCertificateId = consumptionId,
            ProductionSourceSlice = prodQuantity.ToSliceId(),
            ConsumptionSourceSlice = consQuantity.ToSliceId(),
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
