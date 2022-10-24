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

internal static class Helper
{
    private static IEventSerializer serializer = new JsonEventSerializer();
    private static Lazy<Group> lazyGroup = new Lazy<Group>(() => Group.Create(), true);
    internal static Group Group { get => lazyGroup.Value; }

    internal static (ConsumptionCertificate certificate, CommitmentParameters parameters) ConsumptionIssued(Key ownerKey, long quantity)
    {
        var quantityCommitmentParameters = Group.Commit(quantity);
        var gsrnCommitmentParameters = Group.Commit(new Fixture().Create<long>());

        var e = new ConsumptionIssuedEvent(
                new("", Guid.NewGuid()),
                new TimePeriod(
                    DateTimeOffset.Now,
                    DateTimeOffset.Now.AddHours(1)),
                "DK1",
                gsrnCommitmentParameters.Commitment,
                quantityCommitmentParameters.Commitment,
                ownerKey.PublicKey.Export(KeyBlobFormat.RawPublicKey)
                );

        var cert = new ConsumptionCertificate();
        cert.Apply(e);

        return (cert, quantityCommitmentParameters);
    }


    internal static (ProductionCertificate certificate, CommitmentParameters parameters) ProductionIssued(Key ownerKey, long quantity)
    {
        var quantityCommitmentParameters = Group.Commit(quantity);
        var gsrnCommitmentParameters = Group.Commit(new Fixture().Create<long>());

        var e = new ProductionIssuedEvent(
                new("", Guid.NewGuid()),
                new TimePeriod(
                    DateTimeOffset.Now,
                    DateTimeOffset.Now.AddHours(1)),
                "DK1",
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

    internal static (Guid allocationId, ProductionCertificate certificate, CommitmentParameters quantityParams) ProductionAllocated(Key ownerKey, CommitmentParameters quantityParameters, (ProductionCertificate cert, CommitmentParameters parameters) productionTuple, FederatedStreamId consumptionId)
    {
        Guid allocationId = Guid.NewGuid(); ;

        var e = CreateProductionAllocatedEvent(allocationId, productionTuple.cert.Id, consumptionId, quantityParameters, productionTuple.parameters);
        productionTuple.cert.Apply(e.e);

        return (allocationId, productionTuple.cert, e.transfer);
    }


    internal static ConsumptionAllocatedRequest CreateAllocation(
        Guid allocationId,
        FederatedStreamId productionId,
        FederatedStreamId consumptionId,
        CommitmentParameters sourceParameters,
        CommitmentParameters quantityParameters,
        Key signerKey
        )
    {
        var (e, transferParamerters, remainderParameters) = CreateAllocatedEvent(allocationId, productionId, consumptionId, quantityParameters, sourceParameters);
        var serializedEvent = serializer.Serialize(e);
        var signature = NSec.Cryptography.Ed25519.Ed25519.Sign(signerKey, serializedEvent);
        var request = new ConsumptionAllocatedRequest(new SliceParameters(
                sourceParameters,
                transferParamerters,
                remainderParameters
            ), e, signature);

        return request;
    }

    internal static (ConsumptionAllocatedEvent e, CommitmentParameters transfer, CommitmentParameters remainder) CreateAllocatedEvent(Guid allocationId, FederatedStreamId productionId, FederatedStreamId consumptionId, CommitmentParameters quantityParameters, CommitmentParameters sourceParameters)
    {
        var remainderParameters = Group.Commit(sourceParameters.m - quantityParameters.m);

        var e = new ConsumptionAllocatedEvent(
                allocationId,
                productionId,
                consumptionId,
                new Slice(
                    sourceParameters.Commitment,
                    quantityParameters.Commitment,
                    remainderParameters.Commitment,
                    (sourceParameters.r - (quantityParameters.r + remainderParameters.r)).MathMod(Group.q)
                ));

        return (e, quantityParameters, remainderParameters);
    }

    internal static ProductionAllocatedRequest CreateProductionAllocation(
    FederatedStreamId productionId,
    FederatedStreamId consumptionId,
    CommitmentParameters sourceParameters,
    CommitmentParameters quantityParameters,
    Key signerKey
    )
    {
        var allocationId = Guid.NewGuid();
        var (e, transferParamerters, remainderParameters) = CreateProductionAllocatedEvent(allocationId, productionId, consumptionId, quantityParameters, sourceParameters);
        var serializedEvent = serializer.Serialize(e);
        var signature = NSec.Cryptography.Ed25519.Ed25519.Sign(signerKey, serializedEvent);
        var request = new ProductionAllocatedRequest(new SliceParameters(
                sourceParameters,
                transferParamerters,
                remainderParameters
            ), e, signature);

        return request;
    }

    internal static (ProductionAllocatedEvent e, CommitmentParameters transfer, CommitmentParameters remainder) CreateProductionAllocatedEvent(Guid allocationId, FederatedStreamId productionId, FederatedStreamId consumptionId, CommitmentParameters quantityParameters, CommitmentParameters sourceParameters)
    {
        var remainderParameters = Group.Commit(sourceParameters.m - quantityParameters.m);

        var e = new ProductionAllocatedEvent(
                allocationId,
                productionId,
                consumptionId,
                new Slice(
                    sourceParameters.Commitment,
                    quantityParameters.Commitment,
                    remainderParameters.Commitment,
                    (sourceParameters.r - (quantityParameters.r + remainderParameters.r)).MathMod(Group.q)
                ));

        return (e, quantityParameters, remainderParameters);
    }
}
