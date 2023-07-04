using System;
using System.Security.Cryptography;
using AutoFixture;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.HierarchicalDeterministicKeys;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Tests;

internal static class FakeRegister
{
    const string Registry = "OurReg";

    private static V1.DateInterval _defaultPeriod = new V1.DateInterval()
    {
        Start = Timestamp.FromDateTimeOffset(new DateTimeOffset(2022, 09, 25, 12, 0, 0, TimeSpan.Zero)),
        End = Timestamp.FromDateTimeOffset(new DateTimeOffset(2022, 09, 25, 13, 0, 0, TimeSpan.Zero))
    };

    public static Registry.V1.Transaction SignTransaction(Common.V1.FederatedStreamId id, IMessage @event, IPrivateKey signerKey)
    {
        var header = new Registry.V1.TransactionHeader()
        {
            FederatedStreamId = id,
            PayloadType = @event.Descriptor.FullName,
            PayloadSha512 = ByteString.CopyFrom(Sha512.Sha512.Hash(@event.ToByteArray())),
            Nonce = Guid.NewGuid().ToString(),
        };

        var transaction = new Registry.V1.Transaction()
        {
            Header = header,
            HeaderSignature = ByteString.CopyFrom(signerKey.Sign(header.ToByteArray())),
            Payload = @event.ToByteString()
        };

        return transaction;
    }


    internal static (GranularCertificate certificate, SecretCommitmentInfo parameters) ConsumptionIssued(IPublicKey ownerKey, uint quantity, string area = "DK1", V1.DateInterval? periodOverride = null)
    {
        var id = CreateFederatedId();
        var quantityCommitmentParameters = new SecretCommitmentInfo(quantity);
        var gsrnHash = SHA256.HashData(BitConverter.GetBytes(new Fixture().Create<ulong>()));

        var @event = CreateConsumptionIssuedEvent(
                quantityCommitmentParameters.ToProtoCommitment(id.StreamId.Value),
                ownerKey.ToProto(),
                area,
                periodOverride);

        var cert = new GranularCertificate(@event);

        return (cert, quantityCommitmentParameters);
    }

    internal static (GranularCertificate certificate, SecretCommitmentInfo parameters) ProductionIssued(IPublicKey ownerKey, uint quantity, string area = "DK1", V1.DateInterval? periodOverride = null)
    {
        var id = CreateFederatedId();
        var gsrnHash = SHA256.HashData(BitConverter.GetBytes(new Fixture().Create<ulong>()));
        var quantityCommitmentParameters = new SecretCommitmentInfo(quantity);

        var @event = CreateProductionIssuedEvent(
                quantityCommitmentParameters.ToProtoCommitment(id.StreamId.Value),
                ownerKey.ToProto(),
                false,
                null,
                area,
                periodOverride);

        var cert = new GranularCertificate(@event);

        return (cert, quantityCommitmentParameters);
    }

    internal static Guid Allocated(this GranularCertificate prodCert, GranularCertificate consCert, SecretCommitmentInfo produtionParameters, SecretCommitmentInfo sourceParameters, Guid? allocationIdOverride = null)
    {
        var allocationId = allocationIdOverride ?? Guid.NewGuid();

        var @event = CreateAllocationEvent(allocationId, prodCert.Id, consCert.Id, produtionParameters, sourceParameters);
        prodCert.Apply(@event);

        return allocationId;
    }

    internal static Guid Allocated(this GranularCertificate consCert, Guid allocationId, GranularCertificate prodCert, SecretCommitmentInfo produtionParameters, SecretCommitmentInfo sourceParameters)
    {
        var @event = CreateAllocationEvent(allocationId, prodCert.Id, consCert.Id, produtionParameters, sourceParameters);
        consCert.Apply(@event);

        return allocationId;
    }

    internal static void Claimed(this GranularCertificate certificate, Guid allocationId)
    {
        var @event = CreateClaimedEvent(allocationId, certificate.Id);
        certificate.Apply(@event);
    }

    internal static V1.Commitment InvalidCommitment(uint quantity = 150, string label = "hello")
    {
        var privateCommitment = new SecretCommitmentInfo(quantity);
        var anotherCommitmentForInvalidRangeProof = new SecretCommitmentInfo(quantity);
        return new V1.Commitment
        {
            Content = ByteString.CopyFrom(privateCommitment.Commitment.C),
            RangeProof = ByteString.CopyFrom(anotherCommitmentForInvalidRangeProof.CreateRangeProof(label))
        };
    }

    internal static V1.IssuedEvent CreateConsumptionIssuedEvent(
        V1.Commitment? quantityCommitmentOverride = null,
        V1.PublicKey? ownerKeyOverride = null,
        string? gridAreaOverride = null,
        V1.DateInterval? periodOverride = null
        )
    {
        var id = CreateFederatedId();
        var owner = ownerKeyOverride ?? Algorithms.Secp256k1.GenerateNewPrivateKey().PublicKey.ToProto();
        var gsrnHash = SHA256.HashData(BitConverter.GetBytes(5700000000000001));
        var quantityCommmitment = new SecretCommitmentInfo(150).ToProtoCommitment(id.StreamId.Value);

        return new V1.IssuedEvent()
        {
            CertificateId = id,
            Type = V1.GranularCertificateType.Consumption,
            Period = periodOverride ?? _defaultPeriod,
            GridArea = gridAreaOverride ?? "DK1",
            AssetIdHash = ByteString.CopyFrom(gsrnHash),
            QuantityCommitment = quantityCommitmentOverride ?? quantityCommmitment,
            OwnerPublicKey = owner,
        };
    }

    internal static V1.IssuedEvent CreateProductionIssuedEvent(
        V1.Commitment? quantityCommitmentOverride = null,
        V1.PublicKey? ownerKeyOverride = null,
        bool publicQuantity = false,
        SecretCommitmentInfo? publicQuantityCommitmentOverride = null,
        string? gridAreaOverride = null,
        V1.DateInterval? periodOverride = null
        )
    {
        var id = CreateFederatedId();
        var owner = ownerKeyOverride ?? Algorithms.Secp256k1.GenerateNewPrivateKey().PublicKey.ToProto();
        var gsrnHash = SHA256.HashData(BitConverter.GetBytes(5700000000000001));
        var quantityCommmitmentParams = new SecretCommitmentInfo(150);
        var quantityCommmitment = quantityCommmitmentParams.ToProtoCommitment(id.StreamId.Value);

        var @event = new V1.IssuedEvent()
        {
            CertificateId = id,
            Type = V1.GranularCertificateType.Production,
            Period = periodOverride ?? _defaultPeriod,
            GridArea = gridAreaOverride ?? "DK1",
            AssetIdHash = ByteString.CopyFrom(gsrnHash),
            QuantityCommitment = quantityCommitmentOverride ?? quantityCommmitment,
            OwnerPublicKey = owner,
        };

        @event.Attributes.Add(new V1.Attribute { Key = "FuelCode", Value = "F01050100" });
        @event.Attributes.Add(new V1.Attribute { Key = "TechCode", Value = "T020002" });
        // QuantityPublication = publicQuantity ? (publicQuantityCommitmentOverride ?? quantityCommmitmentParams).ToProto() : null

        return @event;
    }

    internal static V1.TransferredEvent CreateTransferEvent(
    GranularCertificate certificate,
    SecretCommitmentInfo sourceSliceParameters,
    V1.PublicKey newOwner
    )
    {
        return new V1.TransferredEvent
        {
            CertificateId = certificate.Id,
            SourceSliceHash = sourceSliceParameters.ToSliceId(),
            NewOwner = newOwner
        };
    }

    public static V1.SlicedEvent CreateSliceEvent(Common.V1.FederatedStreamId id,
                                                   SecretCommitmentInfo sourceParams,
                                                   uint quantity,
                                                   IPublicKey ownerKey,
                                                   V1.PublicKey? newOwnerOverride = null,
                                                   ByteString? sumOverride = null)
    {
        var slice = new SecretCommitmentInfo(quantity);
        var remainder = new SecretCommitmentInfo(sourceParams.Message - quantity);

        var newOwner = newOwnerOverride ?? Algorithms.Secp256k1.GenerateNewPrivateKey().PublicKey.ToProto();

        var @event = new V1.SlicedEvent
        {
            CertificateId = id,
            SourceSliceHash = sourceParams.ToSliceId(),
            SumProof = sumOverride ?? ByteString.CopyFrom(SecretCommitmentInfo.CreateEqualityProof(sourceParams, slice + remainder, id.StreamId.Value))
        };

        @event.NewSlices.Add(new V1.SlicedEvent.Types.Slice
        {
            Quantity = slice.ToProtoCommitment(id.StreamId.Value),
            NewOwner = newOwner
        });
        @event.NewSlices.Add(new V1.SlicedEvent.Types.Slice
        {
            Quantity = remainder.ToProtoCommitment(id.StreamId.Value),
            NewOwner = ownerKey.ToProto()
        });
        return @event;
    }


    internal static V1.AllocatedEvent CreateAllocationEvent(
        Guid allocationId,
        Common.V1.FederatedStreamId productionId,
        Common.V1.FederatedStreamId consumptionId,
        SecretCommitmentInfo productionSlice,
        SecretCommitmentInfo consumptionSlice,
        byte[]? overwrideEqualityProof = null
        )
    {
        return new V1.AllocatedEvent()
        {
            AllocationId = allocationId.ToProto(),
            ProductionCertificateId = productionId,
            ConsumptionCertificateId = consumptionId,
            ProductionSourceSliceHash = productionSlice.ToSliceId(),
            ConsumptionSourceSliceHash = consumptionSlice.ToSliceId(),
            EqualityProof = ByteString.CopyFrom(overwrideEqualityProof ?? SecretCommitmentInfo.CreateEqualityProof(productionSlice, consumptionSlice, allocationId.ToString()))
        };
    }

    internal static V1.ClaimedEvent CreateClaimedEvent(
       Guid allocationId,
       Common.V1.FederatedStreamId certificateId
       )
    {
        return new V1.ClaimedEvent()
        {
            CertificateId = certificateId,
            AllocationId = allocationId.ToProto()
        };
    }

    private static Common.V1.FederatedStreamId CreateFederatedId() => new Common.V1.FederatedStreamId
    {
        Registry = Registry,
        StreamId = new Common.V1.Uuid
        {
            Value = Guid.NewGuid().ToString()
        }
    };
}
