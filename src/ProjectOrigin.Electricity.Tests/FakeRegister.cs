using Google.Protobuf;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.Electricity.Production;
using ProjectOrigin.PedersenCommitment;
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

    // internal static (CommitmentParameters transfer, CommitmentParameters remainder) Transferred(this ProductionCertificate cert, CommitmentParameters sourceParams, long quantity, PublicKey newOwner)
    // {
    //     var e = CreateTransferEvent(cert.Id, sourceParams, quantity, newOwner.Export(KeyBlobFormat.RawPublicKey));

    //     cert.Apply(e.e);

    //     return (e.transfer, e.remainder);
    // }

    internal static VerificationRequest<ProductionCertificate, V1.TransferredEvent> CreateTransfer(
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

        return new VerificationRequest<ProductionCertificate, V1.TransferredEvent>(
            exists ? certificate : null,
            @event,
            Sign(signerKey, @event)
        );
    }

    internal static VerificationRequest<ProductionCertificate, V1.SlicedEvent> CreateSlices(
        ProductionCertificate certificate,
        CommitmentParameters sourceParams,
        int quantity,
        Key ownerKey,
        V1.PublicKey? newOwnerOverride = null,
        ByteString? sumOverride = null,
        bool exists = true)
    {
        var @event = CreateSliceEvent(certificate.Id, sourceParams, quantity, ownerKey, newOwnerOverride, sumOverride);

        return new VerificationRequest<ProductionCertificate, V1.SlicedEvent>(
            exists ? certificate : null,
            @event,
            Sign(ownerKey, @event)
        );
    }

    internal static VerificationRequest<ConsumptionCertificate, V1.SlicedEvent> CreateSlices(
    ConsumptionCertificate certificate,
    CommitmentParameters sourceParams,
    int quantity,
    Key ownerKey,
    V1.PublicKey? newOwnerOverride = null,
    ByteString? sumOverride = null,
    bool exists = true)
    {
        var @event = CreateSliceEvent(certificate.Id, sourceParams, quantity, ownerKey, newOwnerOverride, sumOverride);

        return new VerificationRequest<ConsumptionCertificate, V1.SlicedEvent>(
            exists ? certificate : null,
            @event,
            Sign(ownerKey, @event)
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



    // internal static CommandStep<V1.TransferProductionSliceCommand.Types.ProductionSliceTransferredEvent> CreateTransfer(
    //    Register.V1.FederatedStreamId id,
    //    CommitmentParameters sourceParameters,
    //    long quantity,
    //    Key signerKey,
    //    CommitmentParameters? sourceParametersOverride = null,
    //    CommitmentParameters? transferParametersOverride = null,
    //    CommitmentParameters? remainderParametersOverride = null,
    //    long quantityOffset = 0,
    //    byte[]? newOwnerOverride = null
    //    )
    // {
    //     var newOwner = newOwnerOverride ?? Key.Create(SignatureAlgorithm.Ed25519).PublicKey.Export(KeyBlobFormat.RawPublicKey);

    //     var (e, transferParamerters, remainderParameters) = CreateTransferEvent(id, sourceParameters, quantity, newOwner, quantityOffset);

    //     return new CommandStep<V1.TransferProductionSliceCommand.Types.ProductionSliceTransferredEvent>(
    //         id,
    //         SignEvent(signerKey, e),
    //         typeof(ProductionCertificate),
    //         new V1.SliceProof()
    //         {
    //             Source = (sourceParametersOverride ?? sourceParameters).ToProto(),
    //             Quantity = (transferParametersOverride ?? transferParamerters).ToProto(),
    //             Remainder = (remainderParametersOverride ?? remainderParameters).ToProto(),
    //         }
    //     );
    // }

    // internal static (V1.TransferProductionSliceCommand.Types.ProductionSliceTransferredEvent e, CommitmentParameters transfer, CommitmentParameters remainder) CreateTransferEvent(Register.V1.FederatedStreamId id, CommitmentParameters sourceParameters, long quantity, byte[] newOwner, long quantityOffset = 0)
    // {
    //     var transferParameters = Group.Commit(quantity);
    //     var remainderParameters = Group.Commit(sourceParameters.m - quantity + quantityOffset);

    //     var e = new V1.TransferProductionSliceCommand.Types.ProductionSliceTransferredEvent()
    //     {
    //         CertificateId = id.ToProto(),
    //         Slice = CreateSlice(sourceParameters, transferParameters, remainderParameters),
    //         NewOwner = ByteString.CopyFrom(newOwner)
    //     };

    //     return (e, transferParameters, remainderParameters);
    // }

    internal static VerificationRequest<ProductionCertificate, V1.ClaimedEvent> CreateProductionClaim(
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

        return new VerificationRequest<ProductionCertificate, V1.ClaimedEvent>(
            exists ? productionCertificate : null,
            @event,
            Sign(signerKey, @event),
            otherExists ? new(){
                {consumptionCertificate.Id, consumptionCertificate}
            } : null
        );
    }

    // internal static CommandStep<V1.ClaimCommand.Types.ClaimedEvent> CreateConsumptionClaim(Register.V1.FederatedStreamId certificateId, Guid allocationId, Key signerKey)
    // {
    //     var e = new V1.ClaimCommand.Types.ClaimedEvent()
    //     {
    //         CertificateId = certificateId.ToProto(),
    //         AllocationId = allocationId.ToProto()
    //     };

    //     return new CommandStep<V1.ClaimCommand.Types.ClaimedEvent>(
    //         certificateId,
    //         SignEvent(signerKey, e),
    //         typeof(ConsumptionCertificate)
    //     );
    // }

    // internal static CommandStep<V1.ClaimCommand.Types.AllocatedEvent> CreateConsumptionAllocationRequest(
    //     Guid allocationId,
    //     Register.V1.FederatedStreamId productionId,
    //     Register.V1.FederatedStreamId consumptionId,
    //     CommitmentParameters sourceParameters,
    //     CommitmentParameters quantityParameters,
    //     Key signerKey
    //     )
    // {
    //     var (e, transferParamerters, remainderParameters) = CreateConsumptionAllocatedEvent(allocationId, productionId, consumptionId, quantityParameters, sourceParameters);

    //     return new CommandStep<V1.ClaimCommand.Types.AllocatedEvent>(
    //         consumptionId,
    //         SignEvent(signerKey, e),
    //         typeof(ConsumptionCertificate),
    //         new V1.SliceProof()
    //         {
    //             Source = (sourceParameters).ToProto(),
    //             Quantity = (transferParamerters).ToProto(),
    //             Remainder = (remainderParameters).ToProto(),
    //         }
    //     );
    // }

    internal static VerificationRequest<ConsumptionCertificate, V1.AllocatedEvent> CreateConsumptionAllocationRequest(
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

        return new VerificationRequest<ConsumptionCertificate, V1.AllocatedEvent>(
            exists ? consumption : null,
            @event,
            Sign(signerKey, @event),
            otherExists ? new(){
                {production.Id, production}
            } : null
        );
    }

    internal static VerificationRequest<ProductionCertificate, V1.AllocatedEvent> CreateProductionAllocationRequest(
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

        return new VerificationRequest<ProductionCertificate, V1.AllocatedEvent>(
            exists ? production : null,
            @event,
            Sign(signerKey, @event),
            otherExists ? new(){
                {consumption.Id, consumption}
            } : null
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

    // internal static (V1.ClaimCommand.Types.AllocatedEvent e, CommitmentParameters transfer, CommitmentParameters remainder) CreateConsumptionAllocatedEvent(Guid allocationId, Register.V1.FederatedStreamId productionId, Register.V1.FederatedStreamId consumptionId, CommitmentParameters quantityParameters, CommitmentParameters sourceParameters)
    // {
    //     var remainderParameters = Group.Commit(sourceParameters.m - quantityParameters.m);

    //     var e = new V1.ClaimCommand.Types.AllocatedEvent()
    //     {
    //         AllocationId = allocationId.ToProto(),
    //         ProductionCertificateId = productionId.ToProto(),
    //         ConsumptionCertificateId = consumptionId.ToProto(),
    //         Slice = CreateSlice(sourceParameters, quantityParameters, remainderParameters)
    //     };

    //     return (e, quantityParameters, remainderParameters);
    // }

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

    internal static VerificationRequest<ConsumptionCertificate, V1.ConsumptionIssuedEvent> CreateConsumptionIssuedRequest(
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

        return new VerificationRequest<ConsumptionCertificate, V1.ConsumptionIssuedEvent>(
            exists ? new(@event) : null,
            @event,
            Sign(signerKey, @event)
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

    internal static VerificationRequest<ProductionCertificate, V1.ProductionIssuedEvent> CreateProductionIssuedRequest(
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

        return new VerificationRequest<ProductionCertificate, V1.ProductionIssuedEvent>(
            exists ? new(@event) : null,
            @event,
            Sign(signerKey, @event)
        );
    }

    private static byte[] Sign(Key signerKey, IMessage e)
    {
        return NSec.Cryptography.Ed25519.Ed25519.Sign(signerKey, e.ToByteArray());
    }

    // private static V1.Slice CreateSlice(CommitmentParameters sourceParameters, CommitmentParameters transferParameters, CommitmentParameters remainderParameters)
    // {
    //     return new V1.Slice()
    //     {
    //         Source = (sourceParameters.Commitment).ToProto(),
    //         Quantity = (transferParameters.Commitment).ToProto(),
    //         Remainder = (remainderParameters.Commitment).ToProto(),
    //         ZeroR = ByteString.CopyFrom(((sourceParameters.r - (transferParameters.r + remainderParameters.r)).MathMod(Group.q)).ToByteArray())
    //     };
    // }
}
