using System;
using System.Linq;
using System.Security.Cryptography;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ProjectOrigin.Common.V1;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Example;

public class ProtoEventBuilder
{
    public required string GridArea { get; init; }
    public required DateTimeOffset Start { get; init; }
    public required DateTimeOffset End { get; init; }

    public Electricity.V1.IssuedEvent CreateConsumptionIssuedEvent(FederatedStreamId certId, SecretCommitmentInfo commitmentInfo, IPublicKey ownerKey)
    {
        var @event = new Electricity.V1.IssuedEvent
        {
            CertificateId = certId,
            Type = Electricity.V1.GranularCertificateType.Consumption,
            Period = new Electricity.V1.DateInterval
            {
                Start = Timestamp.FromDateTimeOffset(Start),
                End = Timestamp.FromDateTimeOffset(End)
            },
            GridArea = GridArea,
            AssetIdHash = ByteString.Empty,
            QuantityCommitment = CommitmentToProto(certId, commitmentInfo),
            OwnerPublicKey = new Electricity.V1.PublicKey
            {
                Content = ByteString.CopyFrom(ownerKey.Export())
            }
        };

        return @event;
    }

    public Electricity.V1.IssuedEvent CreateProductionIssuedEvent(FederatedStreamId certId, SecretCommitmentInfo commitmentInfo, IPublicKey ownerKey)
    {
        var @event = new Electricity.V1.IssuedEvent
        {
            CertificateId = certId,
            Type = Electricity.V1.GranularCertificateType.Production,
            Period = new Electricity.V1.DateInterval
            {
                Start = Timestamp.FromDateTimeOffset(Start),
                End = Timestamp.FromDateTimeOffset(End)
            },
            GridArea = GridArea,
            AssetIdHash = ByteString.Empty,
            QuantityCommitment = CommitmentToProto(certId, commitmentInfo),
            OwnerPublicKey = new Electricity.V1.PublicKey
            {
                Content = ByteString.CopyFrom(ownerKey.Export())
            }
        };

        @event.Attributes.Add(new V1.Attribute
        {
            Key = "TechCode",
            Value = "T010101"
        });

        @event.Attributes.Add(new V1.Attribute
        {
            Key = "FuelCode",
            Value = "F010101"
        });

        return @event;
    }

    public Electricity.V1.SlicedEvent CreateSliceEvent(FederatedStreamId certId, IPublicKey newOwnerKey, SecretCommitmentInfo sourceSlice, params SecretCommitmentInfo[] slices)
    {
        var sumOfNewSlices = slices.Aggregate((left, right) => left + right);
        var equalityProof = SecretCommitmentInfo.CreateEqualityProof(sourceSlice, sumOfNewSlices, certId.StreamId.Value);


        var @event = new Electricity.V1.SlicedEvent
        {
            CertificateId = certId,
            SourceSliceHash = ToSliceId(sourceSlice.Commitment),
            SumProof = ByteString.CopyFrom(equalityProof)
        };

        foreach (var slice in slices)
        {
            @event.NewSlices.Add(new V1.SlicedEvent.Types.Slice
            {
                Quantity = CommitmentToProto(certId, slice),
                NewOwner = new V1.PublicKey
                {
                    Content = ByteString.CopyFrom(newOwnerKey.Export())
                }
            });
        }

        return @event;
    }

    public Electricity.V1.AllocatedEvent CreateAllocatedEvent(Guid allocationId, FederatedStreamId prodCertId, FederatedStreamId consCertId, SecretCommitmentInfo prodComtInfo, SecretCommitmentInfo consComtInfo)
    {
        var equalityProof = SecretCommitmentInfo.CreateEqualityProof(prodComtInfo, consComtInfo, allocationId.ToString());

        return new Electricity.V1.AllocatedEvent
        {
            AllocationId = new Common.V1.Uuid { Value = allocationId.ToString() },
            ProductionCertificateId = prodCertId,
            ConsumptionCertificateId = consCertId,
            ProductionSourceSliceHash = ToSliceId(prodComtInfo.Commitment),
            ConsumptionSourceSliceHash = ToSliceId(consComtInfo.Commitment),
            EqualityProof = ByteString.CopyFrom(equalityProof)
        };
    }

    public Electricity.V1.ClaimedEvent CreateClaimEvent(Guid allocationId, FederatedStreamId certificateId)
    {
        return new Electricity.V1.ClaimedEvent
        {
            AllocationId = new Common.V1.Uuid { Value = allocationId.ToString() },
            CertificateId = certificateId
        };
    }

    public FederatedStreamId ToCertId(string registry, Guid certId)
    {
        return new Common.V1.FederatedStreamId
        {
            Registry = registry,
            StreamId = new Common.V1.Uuid
            {
                Value = certId.ToString()
            },
        };
    }

    private static V1.Commitment CommitmentToProto(FederatedStreamId certId, SecretCommitmentInfo commitmentInfo)
    {
        return new Electricity.V1.Commitment
        {
            Content = ByteString.CopyFrom(commitmentInfo.Commitment.C),
            RangeProof = ByteString.CopyFrom(commitmentInfo.CreateRangeProof(certId.StreamId.Value))
        };
    }

    private ByteString ToSliceId(PedersenCommitment.Commitment commitment)
    {
        return ByteString.CopyFrom(SHA256.HashData(commitment.C));
    }
}
