using System.Numerics;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Consumption.Requests;
using ProjectOrigin.Electricity.Production;
using ProjectOrigin.Electricity.Production.Requests;
using ProjectOrigin.Electricity.Shared;
using ProjectOrigin.Electricity.Shared.Internal;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.RequestProcessor.Interfaces;
using ProjectOrigin.RequestProcessor.Services;

namespace ProjectOrigin.Electricity.Tests;

internal static class FakeRegister
{
    private static IEventSerializer serializer = new JsonEventSerializer();
    private static Lazy<Group> lazyGroup = new Lazy<Group>(() => Group.Create(), true);
    internal static Group Group { get => lazyGroup.Value; }

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
        var e = new ProductionClaimedEvent(certificate.Id, allocationId);
        certificate.Apply(e);
    }

    internal static (CommitmentParameters transfer, CommitmentParameters remainder) Transferred(this ProductionCertificate cert, CommitmentParameters sourceParams, long quantity, PublicKey newOwner)
    {
        var e = CreateTransferEvent(cert.Id, sourceParams, quantity, newOwner.Export(KeyBlobFormat.RawPublicKey));

        cert.Apply(e.e);

        return (e.transfer, e.remainder);
    }

    internal static ProductionSliceTransferredRequest CreateTransfer(
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

        var request = new ProductionSliceTransferredRequest(
            new SliceParameters(
                sourceParametersOverride ?? sourceParameters,
                transferParametersOverride ?? transferParamerters,
                remainderParametersOverride ?? remainderParameters
            ),
            Event: e,
            Signature: Sign(signerKey, e));

        return request;
    }

    internal static (ProductionSliceTransferredEvent e, CommitmentParameters transfer, CommitmentParameters remainder) CreateTransferEvent(FederatedStreamId id, CommitmentParameters sourceParameters, long quantity, byte[] newOwner, long quantityOffset = 0)
    {
        var transferParameters = Group.Commit(quantity);
        var remainderParameters = Group.Commit(sourceParameters.m - quantity + quantityOffset);

        var e = new ProductionSliceTransferredEvent(
                id,
                CreateSlice(sourceParameters, transferParameters, remainderParameters),
                newOwner
                );

        return (e, transferParameters, remainderParameters);
    }

    internal static ProductionClaimedRequest CreateProductionClaim(FederatedStreamId certificateId, Guid allocationId, Key signerKey)
    {
        var e = new ProductionClaimedEvent(certificateId, allocationId);

        return new ProductionClaimedRequest(
            e, Sign(signerKey, e)
        );
    }

    internal static ConsumptionClaimedRequest CreateConsumptionClaim(FederatedStreamId certificateId, Guid allocationId, Key signerKey)
    {
        var e = new ConsumptionClaimedEvent(certificateId, allocationId);

        return new ConsumptionClaimedRequest(
            e, Sign(signerKey, e)
        );
    }

    internal static ConsumptionAllocatedRequest CreateConsumptionAllocationRequest(
        Guid allocationId,
        FederatedStreamId productionId,
        FederatedStreamId consumptionId,
        CommitmentParameters sourceParameters,
        CommitmentParameters quantityParameters,
        Key signerKey
        )
    {
        var (e, transferParamerters, remainderParameters) = CreateConsumptionAllocatedEvent(allocationId, productionId, consumptionId, quantityParameters, sourceParameters);

        var request = new ConsumptionAllocatedRequest(new SliceParameters(
                sourceParameters,
                transferParamerters,
                remainderParameters
            ), e, Sign(signerKey, e));

        return request;
    }

    internal static ProductionAllocatedRequest CreateProductionAllocationRequest(
    FederatedStreamId productionId,
    FederatedStreamId consumptionId,
    CommitmentParameters sourceParameters,
    CommitmentParameters quantityParameters,
    Key signerKey
    )
    {
        var allocationId = Guid.NewGuid();
        var (e, transferParamerters, remainderParameters) = CreateProductionAllocatedEvent(allocationId, productionId, consumptionId, quantityParameters, sourceParameters);

        var request = new ProductionAllocatedRequest(new SliceParameters(
                sourceParameters,
                transferParamerters,
                remainderParameters
            ), e, Sign(signerKey, e));

        return request;
    }

    internal static (ProductionAllocatedEvent e, CommitmentParameters transfer, CommitmentParameters remainder) CreateProductionAllocatedEvent(Guid allocationId, FederatedStreamId productionId, FederatedStreamId consumptionId, CommitmentParameters quantityParameters, CommitmentParameters sourceParameters)
    {
        var remainderParameters = Group.Commit(sourceParameters.m - quantityParameters.m);

        var e = new ProductionAllocatedEvent(
                allocationId,
                productionId,
                consumptionId,
                CreateSlice(sourceParameters, quantityParameters, remainderParameters));

        return (e, quantityParameters, remainderParameters);
    }

    internal static (ConsumptionAllocatedEvent e, CommitmentParameters transfer, CommitmentParameters remainder) CreateConsumptionAllocatedEvent(Guid allocationId, FederatedStreamId productionId, FederatedStreamId consumptionId, CommitmentParameters quantityParameters, CommitmentParameters sourceParameters)
    {
        var remainderParameters = Group.Commit(sourceParameters.m - quantityParameters.m);

        var e = new ConsumptionAllocatedEvent(
                allocationId,
                productionId,
                consumptionId,
                CreateSlice(sourceParameters, quantityParameters, remainderParameters));

        return (e, quantityParameters, remainderParameters);
    }

    internal static (ConsumptionCertificate certificate, CommitmentParameters parameters) ConsumptionIssued(Key ownerKey, long quantity, string area = "DK1", TimePeriod? period = null)
    {
        var quantityCommitmentParameters = Group.Commit(quantity);
        var gsrnCommitmentParameters = Group.Commit(new Fixture().Create<long>());

        var e = new ConsumptionIssuedEvent(
                new("", Guid.NewGuid()),
                period ?? defaultPeriod,
                area,
                gsrnCommitmentParameters.Commitment,
                quantityCommitmentParameters.Commitment,
                ownerKey.PublicKey.Export(KeyBlobFormat.RawPublicKey)
                );

        var cert = new ConsumptionCertificate();
        cert.Apply(e);

        return (cert, quantityCommitmentParameters);
    }

    internal static (ProductionCertificate certificate, CommitmentParameters parameters) ProductionIssued(Key ownerKey, long quantity, string area = "DK1", TimePeriod? period = null)
    {
        var quantityCommitmentParameters = Group.Commit(quantity);
        var gsrnCommitmentParameters = Group.Commit(new Fixture().Create<long>());

        var e = new ProductionIssuedEvent(
                new("", Guid.NewGuid()),
                period ?? defaultPeriod,
                area,
                gsrnCommitmentParameters.Commitment,
                quantityCommitmentParameters.Commitment,
                "F01050100",
                "T020002",
                ownerKey.PublicKey.Export(KeyBlobFormat.RawPublicKey)
                );

        var cert = new ProductionCertificate();
        cert.Apply(e);

        return (cert, quantityCommitmentParameters);
    }

    internal static ConsumptionIssuedRequest CreateConsumptionIssuedRequest(
        JsonEventSerializer serializer,
        Key signerKey,
        CommitmentParameters? gsrnCommitmentOverride = null,
        CommitmentParameters? quantityCommitmentOverride = null,
        byte[]? ownerKeyOverride = null,
        string? gridAreaOverride = null
        )
    {
        var quantityCommitmentParameters = Group.Commit(150);
        var gsrnCommitmentParameters = Group.Commit(5700000000000001);

        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

        var e = new ConsumptionIssuedEvent(
                new FederatedStreamId("registry", Guid.NewGuid()),
                new TimePeriod(
                    DateTimeOffset.Now,
                    DateTimeOffset.Now.AddHours(1)),
                gridAreaOverride ?? "DK1",
                gsrnCommitmentParameters.Commitment,
                quantityCommitmentParameters.Commitment,
                ownerKeyOverride ?? ownerKey.PublicKey.Export(KeyBlobFormat.RawPublicKey)
                );

        var request = new ConsumptionIssuedRequest(
            GsrnParameters: gsrnCommitmentOverride ?? gsrnCommitmentParameters,
            QuantityParameters: quantityCommitmentOverride ?? quantityCommitmentParameters,
            Event: e,
            Signature: Sign(signerKey, e));

        return request;
    }

    internal static ProductionIssuedRequest CreateProductionIssuedRequest(
        JsonEventSerializer serializer,
        Key signerKey,
        CommitmentParameters? gsrnCommitmentOverride = null,
        CommitmentParameters? quantityCommitmentOverride = null,
        byte[]? ownerKeyOverride = null,
        bool publicQuantity = false,
        CommitmentParameters? publicQuantityCommitmentOverride = null,
        string? gridAreaOverride = null
        )
    {
        var quantityCommitmentParameters = Group.Commit(150);
        var gsrnCommitmentParameters = Group.Commit(5700000000000001);

        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

        var e = new ProductionIssuedEvent(
                new FederatedStreamId("", Guid.NewGuid()),
                new TimePeriod(
                    DateTimeOffset.Now,
                    DateTimeOffset.Now.AddHours(1)),
                gridAreaOverride ?? "DK1",
                gsrnCommitmentParameters.Commitment,
                quantityCommitmentParameters.Commitment,
                "F01050100",
                "T020002",
                ownerKeyOverride ?? ownerKey.PublicKey.Export(KeyBlobFormat.RawPublicKey),
                publicQuantity ? publicQuantityCommitmentOverride ?? quantityCommitmentParameters : null
                );

        var request = new ProductionIssuedRequest(
            GsrnCommitmentParameters: gsrnCommitmentOverride ?? gsrnCommitmentParameters,
            QuantityCommitmentParameters: quantityCommitmentOverride ?? quantityCommitmentParameters,
            Event: e,
            Signature: Sign(signerKey, e));

        return request;
    }

    private static byte[] Sign(Key signerKey, object e)
    {
        var serializedEvent = serializer.Serialize(e);
        var signature = NSec.Cryptography.Ed25519.Ed25519.Sign(signerKey, serializedEvent);
        return signature;
    }

    private static Slice CreateSlice(CommitmentParameters sourceParameters, CommitmentParameters transferParameters, CommitmentParameters remainderParameters) => new Slice(
                        sourceParameters.Commitment,
                        transferParameters.Commitment,
                        remainderParameters.Commitment,
                        (sourceParameters.r - (transferParameters.r + remainderParameters.r)).MathMod(Group.q)
                    );
}
