using System.Security.Cryptography;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ProjectOrigin.Common.V1;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Example;

public class Helper
{
    static DateTimeOffset Start = new DateTimeOffset(2023, 1, 1, 12, 0, 0, 0, TimeSpan.Zero);
    private string _area;

    public Helper(string area)
    {
        _area = area;
    }

    public Electricity.V1.ConsumptionIssuedEvent CreateConsumptionIssuedEvent(FederatedStreamId certId, SecretCommitmentInfo commitmentInfo, IPublicKey ownerKey)
    {
        var @event = new Electricity.V1.ConsumptionIssuedEvent
        {
            CertificateId = certId,
            Period = new Electricity.V1.DateInterval
            {
                Start = Timestamp.FromDateTimeOffset(Start),
                End = Timestamp.FromDateTimeOffset(Start.AddHours(1))
            },
            GridArea = _area,
            GsrnHash = ByteString.Empty,
            QuantityCommitment = CommitmentToProto(certId, commitmentInfo),
            OwnerPublicKey = new Electricity.V1.PublicKey
            {
                Content = ByteString.CopyFrom(ownerKey.Export())
            }
        };

        return @event;
    }

    public Electricity.V1.ProductionIssuedEvent CreateProductionIssuedEvent(FederatedStreamId certId, SecretCommitmentInfo commitmentInfo, IPublicKey ownerKey)
    {
        var @event = new Electricity.V1.ProductionIssuedEvent
        {
            CertificateId = certId,
            Period = new Electricity.V1.DateInterval
            {
                Start = Timestamp.FromDateTimeOffset(Start),
                End = Timestamp.FromDateTimeOffset(Start.AddHours(1))
            },
            GridArea = _area,
            TechCode = "T010101",
            FuelCode = "F010101",
            GsrnHash = ByteString.Empty,
            QuantityCommitment = CommitmentToProto(certId, commitmentInfo),
            OwnerPublicKey = new Electricity.V1.PublicKey
            {
                Content = ByteString.CopyFrom(ownerKey.Export())
            }
        };

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

    internal Electricity.V1.AllocatedEvent CreateAllocatedEvent(Guid allocationId, FederatedStreamId prodCertId, FederatedStreamId consCertId, SecretCommitmentInfo prodComtInfo, SecretCommitmentInfo consComtInfo)
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

    public Registry.V1.Transaction SignTransaction(Common.V1.FederatedStreamId streamId, IMessage @event, IPrivateKey signerKey)
    {
        var header = new Registry.V1.TransactionHeader()
        {
            FederatedStreamId = streamId,
            PayloadType = @event.Descriptor.FullName,
            PayloadSha512 = ByteString.CopyFrom(SHA512.HashData(@event.ToByteArray())),
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

    public async Task<Registry.V1.GetTransactionStatusResponse> WaitForCommittedOrTimeout(
        Registry.V1.RegistryService.RegistryServiceClient client,
        Registry.V1.Transaction signedTransaction,
        TimeSpan timeout)
    {
        var began = DateTimeOffset.UtcNow;
        var getTransactionStatus = async () => await client.GetTransactionStatusAsync(this.CreateStatusRequest(signedTransaction));

        while (true)
        {
            var result = await getTransactionStatus();

            if (result.Status == ProjectOrigin.Registry.V1.TransactionState.Committed)
                return result;
            else if (result.Status == ProjectOrigin.Registry.V1.TransactionState.Failed)
                throw new Exception($"Transaction failed ”{result.Status}” with message ”{result.Message}”");

            await Task.Delay(1000);

            if (began + timeout < DateTimeOffset.UtcNow)
            {
                throw new TimeoutException($"Transaction timed out ”{result.Status}” with message ”{result.Message}”");
            }
        }
    }

    private Registry.V1.GetTransactionStatusRequest CreateStatusRequest(Registry.V1.Transaction signedTransaction)
    {
        return new ProjectOrigin.Registry.V1.GetTransactionStatusRequest()
        {
            Id = Convert.ToBase64String(SHA256.HashData(signedTransaction.ToByteArray()))
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
