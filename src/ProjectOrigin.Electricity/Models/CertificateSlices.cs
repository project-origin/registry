using System.Security.Cryptography;
using Google.Protobuf;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Models;

public record AllocationSlice(Commitment Commitment, IPublicKey Owner, Common.V1.Uuid AllocationId, Common.V1.FederatedStreamId ProductionCertificateId, Common.V1.FederatedStreamId ConsumptionCertificateId) : CertificateSlice(Commitment, Owner);

public record CertificateSlice(Commitment Commitment, IPublicKey Owner)
{
    public V1.SliceId Id
    {
        get
        {
            return new V1.SliceId
            {
                Hash = ByteString.CopyFrom(SHA256.HashData(Commitment.C))
            };
        }
    }
}
