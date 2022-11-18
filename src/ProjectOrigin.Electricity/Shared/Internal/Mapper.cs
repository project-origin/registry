using System.Numerics;
using Google.Protobuf;
using NSec.Cryptography;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Register.V1;

namespace ProjectOrigin.Electricity.Shared.Internal;

public static class Mapper
{
    public static Commitment ToModel(V1.Commitment proto)
    {
        return new Commitment(new BigInteger(proto.C.ToByteArray()), Group.Default);
    }

    public static V1.Commitment ToProto(Commitment obj)
    {
        return new V1.Commitment()
        {
            C = ByteString.CopyFrom(obj.C.ToByteArray())
        };
    }

    public static CommitmentParameters ToModel(V1.CommitmentProof proto)
    {
        return new CommitmentParameters(proto.M, new BigInteger(proto.R.ToByteArray()), Group.Default);
    }

    public static V1.CommitmentProof ToProto(CommitmentParameters obj)
    {
        return new V1.CommitmentProof()
        {
            M = (ulong)obj.m,
            R = ByteString.CopyFrom(obj.r.ToByteArray())
        };
    }

    internal static V1.PublicKey ToProto(PublicKey publicKey)
    {
        var bytes = publicKey.Export(KeyBlobFormat.RawPublicKey);
        return new V1.PublicKey()
        {
            Content = ByteString.CopyFrom(bytes)
        };
    }

}
