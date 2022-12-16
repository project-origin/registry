using System.Security.Cryptography;
using Google.Protobuf;
using NSec.Cryptography;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Models;

internal record AllocationSlice(Commitment Commitment, PublicKey Owner, Register.V1.Uuid AllocationId, Register.V1.FederatedStreamId ProductionCertificateId, Register.V1.FederatedStreamId ConsumptionCertificateId) : CertificateSlice(Commitment, Owner);

internal record CertificateSlice(Commitment Commitment, PublicKey Owner)
{
    public V1.SliceId Id
    {
        get
        {
            return new V1.SliceId
            {
                Hash = ByteString.CopyFrom(SHA256.HashData(Commitment.C.ToByteArray()))
            };
        }
    }
}
