using System.Numerics;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity;

internal static class ProtoToModelExtensions
{
    internal static bool VerifyCommitment(this V1.Commitment protoCommitment)
    {
        var commitment = Group.Default.CreateCommitment(new BigInteger(protoCommitment.Content.ToByteArray()));
        return Group.Default.VerifyRangeProof(protoCommitment.RangeProof.Span, commitment);
    }

    internal static bool VerifyPublication(this V1.Commitment commitment, V1.CommitmentPublication publication)
    {
        var cp = Group.Default.CreateParameters(publication.Message, new BigInteger(publication.RValue.ToByteArray()));
        return commitment.Content.Span.SequenceEqual(cp.C.ToByteArray());
    }

    internal static Commitment ToModel(this V1.Commitment proto)
    {
        return Group.Default.CreateCommitment(new BigInteger(proto.Content.ToByteArray()));
    }

    internal static CommitmentParameters ToModel(this V1.CommitmentPublication proto)
    {
        return Group.Default.CreateParameters(proto.Message, new BigInteger(proto.RValue.ToByteArray()));
    }

    internal static Guid ToModel(this Register.V1.Uuid allocationId) => Guid.Parse(allocationId.Value);

    internal static DateInterval ToModel(this V1.DateInterval proto)
    {
        return new DateInterval(
            proto.Start.ToDateTimeOffset(),
            proto.End.ToDateTimeOffset());
    }
}
