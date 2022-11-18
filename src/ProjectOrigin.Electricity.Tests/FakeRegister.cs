using System.Numerics;
using Google.Protobuf;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Production;
using ProjectOrigin.Electricity.Shared;
using ProjectOrigin.Electricity.Shared.Internal;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Register.LineProcessor.Models;

namespace ProjectOrigin.Electricity.Tests;

internal static class FakeRegister
{
    internal static Group Group { get => Group.Default; }
    const string Registry = "OurReg";

    private static TimePeriod defaultPeriod = new TimePeriod(
            new DateTimeOffset(2022, 09, 25, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2022, 09, 25, 13, 0, 0, TimeSpan.Zero));

    internal static (Guid allocationId, CommitmentParameters quantityParams) Allocated(this ProductionCertificate cert, CommitmentParameters sourceParameters, FederatedStreamId consumptionId, CommitmentParameters quantityParameters)
    {
        var allocationId = Guid.NewGuid();

        var e = CreateProductionAllocatedEvent(allocationId, cert.Id, consumptionId, quantityParameters, sourceParameters);
        cert.Apply(e.e);

        return (allocationId, e.transfer);
    }

    internal static (Guid allocationId, CommitmentParameters quantityParams) Allocated(this ConsumptionCertificate cert, Guid allocationId, CommitmentParameters sourceParameters, FederatedStreamId productionId, CommitmentParameters quantityParameters)
    {
        var e = CreateConsumptionAllocatedEvent(allocationId, productionId, cert.Id, quantityParameters, sourceParameters);
        cert.Apply(e.e);

        return (allocationId, e.transfer);
    }

    internal static void Claimed(this ProductionCertificate certificate, Guid allocationId)
    {
        var e = new V1.ClaimCommand.Types.ClaimedEvent()
        {
            CertificateId = certificate.Id,
            AllocationId = allocationId.ToUuid()
        };

        certificate.Apply(e);
    }

    internal static (CommitmentParameters transfer, CommitmentParameters remainder) Transferred(this ProductionCertificate cert, CommitmentParameters sourceParams, long quantity, PublicKey newOwner)
    {
        var e = CreateTransferEvent(cert.Id, sourceParams, quantity, newOwner.Export(KeyBlobFormat.RawPublicKey));

        cert.Apply(e.e);

        return (e.transfer, e.remainder);
    }

    internal static CommandStep<V1.TransferProductionSliceCommand.Types.ProductionSliceTransferredEvent> CreateTransfer(
       FederatedStreamId id,
       CommitmentParameters sourceParameters,
       long quantity,
       Key signerKey,
       CommitmentParameters? sourceParametersOverride = null,
       CommitmentParameters? transferParametersOverride = null,
       CommitmentParameters? remainderParametersOverride = null,
       long quantityOffset = 0,
       byte[]? newOwnerOverride = null
       )
    {
        var newOwner = newOwnerOverride ?? Key.Create(SignatureAlgorithm.Ed25519).PublicKey.Export(KeyBlobFormat.RawPublicKey);

        var (e, transferParamerters, remainderParameters) = CreateTransferEvent(id, sourceParameters, quantity, newOwner, quantityOffset);

        return new CommandStep<V1.TransferProductionSliceCommand.Types.ProductionSliceTransferredEvent>(
            id,
            SignEvent(signerKey, e),
            typeof(ProductionCertificate),
            new V1.SliceProof()
            {
                Source = Mapper.ToProto(sourceParametersOverride ?? sourceParameters),
                Quantity = Mapper.ToProto(transferParametersOverride ?? transferParamerters),
                Remainder = Mapper.ToProto(remainderParametersOverride ?? remainderParameters),
            }
        );
    }

    internal static (V1.TransferProductionSliceCommand.Types.ProductionSliceTransferredEvent e, CommitmentParameters transfer, CommitmentParameters remainder) CreateTransferEvent(FederatedStreamId id, CommitmentParameters sourceParameters, long quantity, byte[] newOwner, long quantityOffset = 0)
    {
        var transferParameters = Group.Commit(quantity);
        var remainderParameters = Group.Commit(sourceParameters.m - quantity + quantityOffset);

        var e = new V1.TransferProductionSliceCommand.Types.ProductionSliceTransferredEvent()
        {
            CertificateId = id,
            Slice = CreateSlice(sourceParameters, transferParameters, remainderParameters),
            NewOwner = ByteString.CopyFrom(newOwner)
        };

        return (e, transferParameters, remainderParameters);
    }

    internal static CommandStep<V1.ClaimCommand.Types.ClaimedEvent> CreateProductionClaim(FederatedStreamId certificateId, Guid allocationId, Key signerKey)
    {
        var e = new V1.ClaimCommand.Types.ClaimedEvent()
        {
            CertificateId = certificateId,
            AllocationId = allocationId.ToUuid()
        };

        return new CommandStep<V1.ClaimCommand.Types.ClaimedEvent>(
            certificateId,
            SignEvent(signerKey, e),
            typeof(ProductionCertificate)
        );
    }

    internal static CommandStep<V1.ClaimCommand.Types.ClaimedEvent> CreateConsumptionClaim(FederatedStreamId certificateId, Guid allocationId, Key signerKey)
    {
        var e = new V1.ClaimCommand.Types.ClaimedEvent()
        {
            CertificateId = certificateId,
            AllocationId = allocationId.ToUuid()
        };

        return new CommandStep<V1.ClaimCommand.Types.ClaimedEvent>(
            certificateId,
            SignEvent(signerKey, e),
            typeof(ConsumptionCertificate)
        );
    }

    internal static CommandStep<V1.ClaimCommand.Types.AllocatedEvent> CreateConsumptionAllocationRequest(
        Guid allocationId,
        FederatedStreamId productionId,
        FederatedStreamId consumptionId,
        CommitmentParameters sourceParameters,
        CommitmentParameters quantityParameters,
        Key signerKey
        )
    {
        var (e, transferParamerters, remainderParameters) = CreateConsumptionAllocatedEvent(allocationId, productionId, consumptionId, quantityParameters, sourceParameters);

        return new CommandStep<V1.ClaimCommand.Types.AllocatedEvent>(
            consumptionId,
            SignEvent(signerKey, e),
            typeof(ConsumptionCertificate),
            new V1.SliceProof()
            {
                Source = Mapper.ToProto(sourceParameters),
                Quantity = Mapper.ToProto(transferParamerters),
                Remainder = Mapper.ToProto(remainderParameters),
            }
        );
    }

    internal static CommandStep<V1.ClaimCommand.Types.AllocatedEvent> CreateProductionAllocationRequest(
    FederatedStreamId productionId,
    FederatedStreamId consumptionId,
    CommitmentParameters sourceParameters,
    CommitmentParameters quantityParameters,
    Key signerKey
    )
    {
        var allocationId = Guid.NewGuid();
        var (e, transferParamerters, remainderParameters) = CreateProductionAllocatedEvent(allocationId, productionId, consumptionId, quantityParameters, sourceParameters);

        return new CommandStep<V1.ClaimCommand.Types.AllocatedEvent>(
            productionId,
            SignEvent(signerKey, e),
            typeof(ProductionCertificate),
            new V1.SliceProof()
            {
                Source = Mapper.ToProto(sourceParameters),
                Quantity = Mapper.ToProto(transferParamerters),
                Remainder = Mapper.ToProto(remainderParameters),
            }
        );
    }

    internal static (V1.ClaimCommand.Types.AllocatedEvent e, CommitmentParameters transfer, CommitmentParameters remainder) CreateProductionAllocatedEvent(Guid allocationId, FederatedStreamId productionId, FederatedStreamId consumptionId, CommitmentParameters quantityParameters, CommitmentParameters sourceParameters)
    {
        var remainderParameters = Group.Commit(sourceParameters.m - quantityParameters.m);

        var e = new V1.ClaimCommand.Types.AllocatedEvent()
        {
            AllocationId = allocationId.ToUuid(),
            ProductionCertificateId = productionId,
            ConsumptionCertificateId = consumptionId,
            Slice = CreateSlice(sourceParameters, quantityParameters, remainderParameters)
        };

        return (e, quantityParameters, remainderParameters);
    }

    internal static (V1.ClaimCommand.Types.AllocatedEvent e, CommitmentParameters transfer, CommitmentParameters remainder) CreateConsumptionAllocatedEvent(Guid allocationId, FederatedStreamId productionId, FederatedStreamId consumptionId, CommitmentParameters quantityParameters, CommitmentParameters sourceParameters)
    {
        var remainderParameters = Group.Commit(sourceParameters.m - quantityParameters.m);

        var e = new V1.ClaimCommand.Types.AllocatedEvent()
        {
            AllocationId = allocationId.ToUuid(),
            ProductionCertificateId = productionId,
            ConsumptionCertificateId = consumptionId,
            Slice = CreateSlice(sourceParameters, quantityParameters, remainderParameters)
        };

        return (e, quantityParameters, remainderParameters);
    }

    internal static (ConsumptionCertificate certificate, CommitmentParameters parameters) ConsumptionIssued(PublicKey ownerKey, long quantity, string area = "DK1", TimePeriod? period = null)
    {
        var id = new FederatedStreamId(Registry, Guid.NewGuid());
        var quantityCommitmentParameters = Group.Commit(quantity);
        var gsrnCommitmentParameters = Group.Commit(new Fixture().Create<long>());

        var e = new V1.IssueConsumptionCommand.Types.ConsumptionIssuedEvent()
        {
            CertificateId = id,
            Period = period ?? defaultPeriod,
            GridArea = area,
            GsrnCommitment = Mapper.ToProto(gsrnCommitmentParameters.Commitment),
            QuantityCommitment = Mapper.ToProto(quantityCommitmentParameters.Commitment),
            OwnerPublicKey = Mapper.ToProto(ownerKey),
        };

        var cert = new ConsumptionCertificate();
        cert.Apply(e);

        return (cert, quantityCommitmentParameters);
    }

    internal static (ProductionCertificate certificate, CommitmentParameters parameters) ProductionIssued(PublicKey ownerKey, long quantity, string area = "DK1", TimePeriod? period = null)
    {
        var id = new FederatedStreamId(Registry, Guid.NewGuid());
        var quantityCommitmentParameters = Group.Commit(quantity);
        var gsrnCommitmentParameters = Group.Commit(new Fixture().Create<long>());

        var e = new V1.IssueProductionCommand.Types.ProductionIssuedEvent()
        {
            CertificateId = id,
            Period = period ?? defaultPeriod,
            GridArea = area,
            FuelCode = "F01050100",
            TechCode = "T020002",
            GsrnCommitment = Mapper.ToProto(gsrnCommitmentParameters.Commitment),
            QuantityCommitment = Mapper.ToProto(quantityCommitmentParameters.Commitment),
            OwnerPublicKey = Mapper.ToProto(ownerKey),
        };

        var cert = new ProductionCertificate();
        cert.Apply(e);

        return (cert, quantityCommitmentParameters);
    }

    internal static CommandStep<V1.IssueConsumptionCommand.Types.ConsumptionIssuedEvent> CreateConsumptionIssuedRequest(
        Key signerKey,
        CommitmentParameters? gsrnCommitmentOverride = null,
        CommitmentParameters? quantityCommitmentOverride = null,
        byte[]? ownerKeyOverride = null,
        string? gridAreaOverride = null
        )
    {
        var id = new FederatedStreamId(Registry, Guid.NewGuid());
        var quantityCommitmentParameters = Group.Commit(150);
        var gsrnCommitmentParameters = Group.Commit(5700000000000001);

        var ownerKey = new V1.PublicKey()
        {
            Content = ByteString.CopyFrom(ownerKeyOverride ?? Key.Create(SignatureAlgorithm.Ed25519).Export(KeyBlobFormat.RawPublicKey))
        };

        var e = new V1.IssueConsumptionCommand.Types.ConsumptionIssuedEvent()
        {
            CertificateId = id,
            Period = new TimePeriod(
                    DateTimeOffset.Now,
                    DateTimeOffset.Now.AddHours(1)),
            GridArea = gridAreaOverride ?? "DK1",
            GsrnCommitment = Mapper.ToProto(gsrnCommitmentParameters.Commitment),
            QuantityCommitment = Mapper.ToProto(quantityCommitmentParameters.Commitment),
            OwnerPublicKey = ownerKey,
        };

        return new CommandStep<V1.IssueConsumptionCommand.Types.ConsumptionIssuedEvent>(
            id,
            SignEvent(signerKey, e),
            typeof(ConsumptionCertificate),
            new V1.IssueConsumptionCommand.Types.ConsumptionIssuedProof()
            {
                GsrnProof = Mapper.ToProto(gsrnCommitmentOverride ?? gsrnCommitmentParameters),
                QuantityProof = Mapper.ToProto(quantityCommitmentOverride ?? quantityCommitmentParameters)
            }
        );
    }

    internal static CommandStep<V1.IssueProductionCommand.Types.ProductionIssuedEvent> CreateProductionIssuedRequest(
        Key signerKey,
        CommitmentParameters? gsrnCommitmentOverride = null,
        CommitmentParameters? quantityCommitmentOverride = null,
        byte[]? ownerKeyOverride = null,
        bool publicQuantity = false,
        CommitmentParameters? publicQuantityCommitmentOverride = null,
        string? gridAreaOverride = null
        )
    {
        var id = new FederatedStreamId(Registry, Guid.NewGuid());
        var quantityCommitmentParameters = Group.Commit(150);
        var gsrnCommitmentParameters = Group.Commit(5700000000000001);

        var ownerKey = new V1.PublicKey()
        {
            Content = ByteString.CopyFrom(ownerKeyOverride ?? Key.Create(SignatureAlgorithm.Ed25519).Export(KeyBlobFormat.RawPublicKey))
        };

        var e = new V1.IssueProductionCommand.Types.ProductionIssuedEvent()
        {
            CertificateId = id,
            Period = new TimePeriod(
                    DateTimeOffset.Now,
                    DateTimeOffset.Now.AddHours(1)),
            GridArea = gridAreaOverride ?? "DK1",
            FuelCode = "F01050100",
            TechCode = "T020002",
            GsrnCommitment = Mapper.ToProto(gsrnCommitmentParameters.Commitment),
            QuantityCommitment = Mapper.ToProto(quantityCommitmentParameters.Commitment),
            OwnerPublicKey = ownerKey,
            QuantityProof = publicQuantity ? Mapper.ToProto(publicQuantityCommitmentOverride ?? quantityCommitmentParameters) : null
        };

        return new CommandStep<V1.IssueProductionCommand.Types.ProductionIssuedEvent>(
            id,
            SignEvent(signerKey, e),
            typeof(ProductionCertificate),
            new V1.IssueProductionCommand.Types.ProductionIssuedProof()
            {
                GsrnProof = Mapper.ToProto(gsrnCommitmentOverride ?? gsrnCommitmentParameters),
                QuantityProof = Mapper.ToProto(quantityCommitmentOverride ?? quantityCommitmentParameters)
            }
        );
    }


    private static SignedEvent<T> SignEvent<T>(Key signerKey, T e) where T : IMessage
    {
        var signature = NSec.Cryptography.Ed25519.Ed25519.Sign(signerKey, e.ToByteArray());
        return new SignedEvent<T>(e, signature);
    }

    private static byte[] Sign(Key signerKey, IMessage e)
    {
        return NSec.Cryptography.Ed25519.Ed25519.Sign(signerKey, e.ToByteArray());
    }

    private static V1.Slice CreateSlice(CommitmentParameters sourceParameters, CommitmentParameters transferParameters, CommitmentParameters remainderParameters)
    {
        return new V1.Slice()
        {
            Source = Mapper.ToProto(sourceParameters.Commitment),
            Quantity = Mapper.ToProto(transferParameters.Commitment),
            Remainder = Mapper.ToProto(remainderParameters.Commitment),
            ZeroR = ByteString.CopyFrom(((sourceParameters.r - (transferParameters.r + remainderParameters.r)).MathMod(Group.q)).ToByteArray())
        };
    }
}
