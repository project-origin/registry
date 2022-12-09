using Google.Protobuf;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Electricity.Production;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.V1;

namespace ProjectOrigin.Electricity.Tests;

internal static class FakeRegister
{
    internal static Group Group { get => Group.Default; }
    const string Registry = "OurReg";

    private static V1.DateInterval _defaultPeriod = new DateInterval(
            new DateTimeOffset(2022, 09, 25, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2022, 09, 25, 13, 0, 0, TimeSpan.Zero)).ToProto();

    internal static Guid Allocated(this ProductionCertificate prodCert, ConsumptionCertificate consCert, CommitmentParameters produtionParameters, CommitmentParameters sourceParameters, Key signer, Guid? allocationIdOverride = null)
    {
        var allocationId = allocationIdOverride ?? Guid.NewGuid();

        var request = CreateProductionAllocationRequest(prodCert, consCert, produtionParameters, sourceParameters, signer, allocationIdOverride: allocationId);
        prodCert.Apply(request.Event);

        return allocationId;
    }

    internal static Guid Allocated(this ConsumptionCertificate consSert, Guid allocationId, ProductionCertificate prodCert, CommitmentParameters produtionParameters, CommitmentParameters sourceParameters, Key signer)
    {
        var request = CreateConsumptionAllocationRequest(allocationId, prodCert, consSert, produtionParameters, sourceParameters, signer);
        consSert.Apply(request.Event);

        return allocationId;
    }

    internal static V1.Commitment InvalidCommitment(ulong quantity = 150)
    {
        return new V1.Commitment
        {
            Content = ByteString.CopyFrom(FakeRegister.Group.Commit(quantity).C.ToByteArray()),
            RangeProof = ByteString.CopyFrom(new Fixture().Create<byte[]>())
        };
    }

    internal static void Claimed(this ProductionCertificate certificate, Guid allocationId)
    {
        var e = new V1.ClaimedEvent()
        {
            CertificateId = certificate.Id,
            AllocationId = allocationId.ToProto()
        };

        certificate.Apply(e);
    }

    internal static VerificationRequest<V1.TransferredEvent> CreateTransfer(
        ProductionCertificate certificate,
        CommitmentParameters sourceSliceParameters,
        V1.PublicKey newOwner,
        Key signerKey,
        bool exists = true
    )
    {
        var @event = new V1.TransferredEvent
        {
            CertificateId = certificate.Id,
            SourceSlice = sourceSliceParameters.ToSliceId(),
            NewOwner = newOwner
        };

        return new VerificationRequest<V1.TransferredEvent>(
            @event,
            Sign(signerKey, @event),
            exists ? new(){
                {certificate.Id, certificate}
            } : new()
        );
    }

    internal static VerificationRequest<V1.SlicedEvent> CreateSlices(
        ProductionCertificate certificate,
        CommitmentParameters sourceParams,
        int quantity,
        Key ownerKey,
        V1.PublicKey? newOwnerOverride = null,
        ByteString? sumOverride = null,
        bool exists = true)
    {
        var @event = CreateSliceEvent(certificate.Id, sourceParams, quantity, ownerKey, newOwnerOverride, sumOverride);

        return new VerificationRequest<V1.SlicedEvent>(
            @event,
            Sign(ownerKey, @event),
            exists ? new(){
                {certificate.Id, certificate}
            } : new()
        );
    }

    internal static VerificationRequest<V1.SlicedEvent> CreateSlices(
    ConsumptionCertificate certificate,
    CommitmentParameters sourceParams,
    int quantity,
    Key ownerKey,
    V1.PublicKey? newOwnerOverride = null,
    ByteString? sumOverride = null,
    bool exists = true)
    {
        var @event = CreateSliceEvent(certificate.Id, sourceParams, quantity, ownerKey, newOwnerOverride, sumOverride);

        return new VerificationRequest<V1.SlicedEvent>(
            @event,
            Sign(ownerKey, @event),
            exists ? new(){
                {certificate.Id, certificate}
            } : new()
        );
    }


    private static V1.SlicedEvent CreateSliceEvent(FederatedStreamId id, CommitmentParameters sourceParams, int quantity, Key ownerKey, V1.PublicKey? newOwnerOverride, ByteString? sumOverride)
    {
        var slice = Group.Commit(quantity);
        var remainder = Group.Commit(sourceParams.Message - quantity);

        var newOwner = newOwnerOverride ?? Key.Create(SignatureAlgorithm.Ed25519).PublicKey.ToProto();

        var @event = new V1.SlicedEvent
        {
            CertificateId = id,
            SourceSlice = sourceParams.ToSliceId(),
            SumProof = sumOverride ?? ByteString.CopyFrom(Group.CreateEqualityProof(sourceParams, slice, remainder))
        };

        @event.NewSlices.Add(new V1.SlicedEvent.Types.Slice
        {
            Quantity = slice.ToProtoCommitment(),
            NewOwner = newOwner
        });
        @event.NewSlices.Add(new V1.SlicedEvent.Types.Slice
        {
            Quantity = remainder.ToProtoCommitment(),
            NewOwner = ownerKey.PublicKey.ToProto()
        });
        return @event;
    }


    internal static VerificationRequest<V1.ClaimedEvent> CreateProductionClaim(
        Guid allocationId,
        ProductionCertificate productionCertificate,
        ConsumptionCertificate consumptionCertificate,
        Key signerKey,
        bool exists = true,
        bool otherExists = true
        )
    {
        var @event = new V1.ClaimedEvent()
        {
            CertificateId = productionCertificate.Id,
            AllocationId = allocationId.ToProto()
        };

        var models = new Dictionary<FederatedStreamId, object>();
        if (exists) models.Add(productionCertificate.Id, productionCertificate);
        if (otherExists) models.Add(consumptionCertificate.Id, consumptionCertificate);

        return new VerificationRequest<V1.ClaimedEvent>(
            @event,
            Sign(signerKey, @event),
            models
        );
    }

    internal static VerificationRequest<V1.ClaimedEvent> CreateConsumptionClaim(
       Guid allocationId,
       ProductionCertificate productionCertificate,
       ConsumptionCertificate consumptionCertificate,
       Key signerKey,
       bool exists = true,
       bool otherExists = true
       )
    {
        var @event = new V1.ClaimedEvent()
        {
            CertificateId = consumptionCertificate.Id,
            AllocationId = allocationId.ToProto()
        };

        var models = new Dictionary<FederatedStreamId, object>();
        if (exists) models.Add(consumptionCertificate.Id, consumptionCertificate);
        if (otherExists) models.Add(productionCertificate.Id, productionCertificate);

        return new VerificationRequest<V1.ClaimedEvent>(
            @event,
            Sign(signerKey, @event),
            models
        );
    }

    internal static VerificationRequest<V1.AllocatedEvent> CreateConsumptionAllocationRequest(
    Guid allocationId,
    ProductionCertificate production,
    ConsumptionCertificate consumption,
    CommitmentParameters productionSlice,
    CommitmentParameters consumptionSlice,
    Key signerKey,
    bool exists = true,
    bool otherExists = true,
     byte[]? overwrideEqualityProof = null
    )
    {
        var @event = CreateAllocationEvent(allocationId, production.Id, consumption.Id, productionSlice, consumptionSlice, overwrideEqualityProof);

        var models = new Dictionary<FederatedStreamId, object>();
        if (exists) models.Add(consumption.Id, consumption);
        if (otherExists) models.Add(production.Id, production);

        return new VerificationRequest<V1.AllocatedEvent>(
            @event,
            Sign(signerKey, @event),
            models
        );
    }

    internal static VerificationRequest<V1.AllocatedEvent> CreateProductionAllocationRequest(
    ProductionCertificate production,
    ConsumptionCertificate consumption,
    CommitmentParameters productionSlice,
    CommitmentParameters consumptionSlice,
    Key signerKey,
    bool exists = true,
    bool otherExists = true,
    byte[]? overwrideEqualityProof = null,
    Guid? allocationIdOverride = null
    )
    {
        var allocationId = allocationIdOverride ?? Guid.NewGuid();
        var @event = CreateAllocationEvent(allocationId, production.Id, consumption.Id, productionSlice, consumptionSlice, overwrideEqualityProof);

        var models = new Dictionary<FederatedStreamId, object>();
        if (exists) models.Add(production.Id, production);
        if (otherExists) models.Add(consumption.Id, consumption);

        return new VerificationRequest<V1.AllocatedEvent>(
            @event,
            Sign(signerKey, @event),
            models
        );
    }

    internal static V1.AllocatedEvent CreateAllocationEvent(
        Guid allocationId,
        Register.V1.FederatedStreamId productionId,
        Register.V1.FederatedStreamId consumptionId,
        CommitmentParameters productionSlice,
        CommitmentParameters consumptionSlice,
        byte[]? overwrideEqualityProof
        )
    {
        return new V1.AllocatedEvent()
        {
            AllocationId = allocationId.ToProto(),
            ProductionCertificateId = productionId,
            ConsumptionCertificateId = consumptionId,
            ProductionSourceSlice = productionSlice.ToSliceId(),
            ConsumptionSourceSlice = consumptionSlice.ToSliceId(),
            EqualityProof = ByteString.CopyFrom(overwrideEqualityProof ?? Group.CreateEqualityProof(consumptionSlice, productionSlice))
        };
    }

    internal static (ConsumptionCertificate certificate, CommitmentParameters parameters) ConsumptionIssued(PublicKey ownerKey, long quantity, string area = "DK1", DateInterval? periodOverride = null)
    {
        var id = CreateFederatedId();
        var quantityCommitmentParameters = Group.Commit(quantity);
        var gsrnCommitmentParameters = Group.Commit(new Fixture().Create<long>());

        var @event = new V1.ConsumptionIssuedEvent()
        {
            CertificateId = id,
            Period = (periodOverride?.ToProto() ?? _defaultPeriod),
            GridArea = area,
            GsrnCommitment = gsrnCommitmentParameters.ToProtoCommitment(),
            QuantityCommitment = quantityCommitmentParameters.ToProtoCommitment(),
            OwnerPublicKey = (ownerKey).ToProto(),
        };

        var cert = new ConsumptionCertificate(@event);

        return (cert, quantityCommitmentParameters);
    }

    internal static (ProductionCertificate certificate, CommitmentParameters parameters) ProductionIssued(PublicKey ownerKey, long quantity, string area = "DK1", DateInterval? periodOverride = null)
    {
        var id = CreateFederatedId();
        var gsrnCommitmentParameters = Group.Commit(new Fixture().Create<long>());
        var quantityCommitmentParameters = Group.Commit(quantity);

        var @event = new V1.ProductionIssuedEvent()
        {
            CertificateId = id,
            Period = (periodOverride?.ToProto() ?? _defaultPeriod),
            GridArea = area,
            FuelCode = "F01050100",
            TechCode = "T020002",
            GsrnCommitment = gsrnCommitmentParameters.ToProtoCommitment(),
            QuantityCommitment = quantityCommitmentParameters.ToProtoCommitment(),
            OwnerPublicKey = ownerKey.ToProto(),
        };

        var cert = new ProductionCertificate(@event);

        return (cert, quantityCommitmentParameters);
    }

    internal static VerificationRequest<V1.ConsumptionIssuedEvent> CreateConsumptionIssuedRequest(
        Key signerKey,
        V1.PublicKey? ownerKeyOverride = null,
        V1.Commitment? gsrnCommitmentOverride = null,
        V1.Commitment? quantityCommitmentOverride = null,
        string? gridAreaOverride = null,
        bool exists = false
        )
    {
        var id = CreateFederatedId();
        var owner = ownerKeyOverride ?? Key.Create(SignatureAlgorithm.Ed25519).PublicKey.ToProto();
        var gsrnCommitment = Group.Commit(5700000000000001).ToProtoCommitment();
        var quantityCommmitment = Group.Commit(150).ToProtoCommitment();

        var @event = new V1.ConsumptionIssuedEvent()
        {
            CertificateId = id,
            Period = _defaultPeriod,
            GridArea = gridAreaOverride ?? "DK1",
            GsrnCommitment = gsrnCommitmentOverride ?? gsrnCommitment,
            QuantityCommitment = quantityCommitmentOverride ?? quantityCommmitment,
            OwnerPublicKey = owner,
        };

        return new VerificationRequest<V1.ConsumptionIssuedEvent>(
            @event,
            Sign(signerKey, @event),
            exists ? new(){
                {id, new ConsumptionCertificate(@event)}
            } : new()
        );
    }

    private static FederatedStreamId CreateFederatedId() => new Register.V1.FederatedStreamId
    {
        Registry = Registry,
        StreamId = new Register.V1.Uuid
        {
            Value = Guid.NewGuid().ToString()
        }
    };

    internal static VerificationRequest<V1.ProductionIssuedEvent> CreateProductionIssuedRequest(
        Key signerKey,
        V1.Commitment? gsrnCommitmentOverride = null,
        V1.Commitment? quantityCommitmentOverride = null,
        V1.PublicKey? ownerKeyOverride = null,
        bool publicQuantity = false,
        CommitmentParameters? publicQuantityCommitmentOverride = null,
        string? gridAreaOverride = null,
        bool exists = false
        )
    {
        var id = CreateFederatedId();
        var owner = ownerKeyOverride ?? Key.Create(SignatureAlgorithm.Ed25519).PublicKey.ToProto();
        var gsrnCommitment = Group.Commit(5700000000000001).ToProtoCommitment();
        var quantityCommmitmentParams = Group.Commit(150);
        var quantityCommmitment = quantityCommmitmentParams.ToProtoCommitment();

        var @event = new V1.ProductionIssuedEvent()
        {
            CertificateId = id,
            Period = _defaultPeriod,
            GridArea = gridAreaOverride ?? "DK1",
            FuelCode = "F01050100",
            TechCode = "T020002",
            GsrnCommitment = gsrnCommitmentOverride ?? gsrnCommitment,
            QuantityCommitment = quantityCommitmentOverride ?? quantityCommmitment,
            OwnerPublicKey = owner,
            QuantityPublication = publicQuantity ? (publicQuantityCommitmentOverride ?? quantityCommmitmentParams).ToProto() : null
        };

        return new VerificationRequest<V1.ProductionIssuedEvent>(
            @event,
            Sign(signerKey, @event),
            exists ? new(){
                {id, new ProductionCertificate(@event)}
            } : new()
        );
    }

    private static byte[] Sign(Key signerKey, IMessage e)
    {
        return NSec.Cryptography.Ed25519.Ed25519.Sign(signerKey, e.ToByteArray());
    }
}
