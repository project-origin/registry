using System.Security.Cryptography;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Tests;

internal static class ModelToProtoExtensions
{
    internal static V1.SliceId ToSliceId(this CommitmentParameters @params)
    {
        return new V1.SliceId()
        {
            Hash = ByteString.CopyFrom(SHA256.HashData(@params.C.ToByteArray()))
        };
    }


    internal static Register.V1.Uuid ToProto(this Guid allocationId)
    {
        return new Register.V1.Uuid()
        {
            Value = allocationId.ToString()
        };
    }

    public static V1.Commitment ToProtoCommitment(this CommitmentParameters obj)
    {
        return new V1.Commitment()
        {
            Content = ByteString.CopyFrom(obj.C.ToByteArray())
        };
    }

    public static V1.CommitmentPublication ToProto(this CommitmentParameters obj)
    {
        return new V1.CommitmentPublication()
        {
            Message = (ulong)obj.Message,
            RValue = ByteString.CopyFrom(obj.RValue.ToByteArray())
        };
    }

    internal static V1.PublicKey ToProto(this PublicKey publicKey)
    {
        var bytes = publicKey.Export(KeyBlobFormat.RawPublicKey);
        return new V1.PublicKey()
        {
            Content = ByteString.CopyFrom(bytes)
        };
    }

    internal static V1.DateInterval ToProto(this DateInterval model)
    {
        return new V1.DateInterval()
        {
            Start = Timestamp.FromDateTimeOffset(model.Start),
            End = Timestamp.FromDateTimeOffset(model.End),
        };
    }
}
