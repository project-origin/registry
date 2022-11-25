using Google.Protobuf;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Client.Models;

internal static class Extensions
{
    public static V1.Commitment ToProtoCommitment(this ShieldedValue sv)
    {
        var commitmentParameters = new CommitmentParameters(sv.Message, sv.R, Group.Default);
        return new V1.Commitment()
        {
            C = ByteString.CopyFrom(commitmentParameters.C.ToByteArray())
        };
    }

    public static V1.CommitmentProof ToProtoCommitmentProof(this ShieldedValue sv)
    {
        var commitmentParameters = new CommitmentParameters(sv.Message, sv.R, Group.Default);

        return new V1.CommitmentProof()
        {
            M = (ulong)commitmentParameters.m,
            R = ByteString.CopyFrom(commitmentParameters.r.ToByteArray())
        };
    }
}
