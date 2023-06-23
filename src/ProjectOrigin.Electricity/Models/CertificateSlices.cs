using System.Security.Cryptography;
using Google.Protobuf;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Models;

public record AllocationSlice(Commitment Commitment, Electricity.V1.PublicKey Owner, Common.V1.Uuid AllocationId, Common.V1.FederatedStreamId ProductionCertificateId, Common.V1.FederatedStreamId ConsumptionCertificateId) : CertificateSlice(Commitment, Owner);

public record CertificateSlice(Commitment Commitment, Electricity.V1.PublicKey Owner)
{
    public ByteString Hash => ByteString.CopyFrom(SHA256.HashData(Commitment.C));
}
