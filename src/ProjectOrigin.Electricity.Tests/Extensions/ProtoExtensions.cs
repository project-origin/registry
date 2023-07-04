using System;
using System.Security.Cryptography;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Tests;

public static class ProtoExtensions
{
    internal static V1.DateInterval AddHours(this V1.DateInterval interval, int hours)
    {
        return new V1.DateInterval()
        {
            Start = Timestamp.FromDateTimeOffset(interval.Start.ToDateTimeOffset().AddHours(hours)),
            End = Timestamp.FromDateTimeOffset(interval.End.ToDateTimeOffset().AddHours(hours))
        };
    }

    internal static ByteString ToSliceId(this SecretCommitmentInfo @params)
    {
        return ByteString.CopyFrom(SHA256.HashData(@params.Commitment.C));
    }

    internal static Common.V1.Uuid ToProto(this Guid allocationId)
    {
        return new Common.V1.Uuid()
        {
            Value = allocationId.ToString()
        };
    }

    public static V1.Commitment ToProtoCommitment(this SecretCommitmentInfo obj, string certId)
    {
        return new V1.Commitment()
        {
            Content = ByteString.CopyFrom(obj.Commitment.C),
            RangeProof = ByteString.CopyFrom(obj.CreateRangeProof(certId))
        };
    }

    public static V1.CommitmentPublication ToProto(this SecretCommitmentInfo obj)
    {
        return new V1.CommitmentPublication()
        {
            Message = (uint)obj.Message,
            RValue = ByteString.CopyFrom(obj.BlindingValue)
        };
    }

    internal static V1.PublicKey ToProto(this IPublicKey publicKey)
    {
        return new V1.PublicKey()
        {
            Content = ByteString.CopyFrom(publicKey.Export()),
            Type = V1.KeyType.Secp256K1
        };
    }
}
