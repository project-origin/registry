using System.Numerics;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity;

internal static class ProtoToModelExtensions
{
    internal static bool VerifyCommitment(this V1.Commitment protoCommitment, string label)
    {
        var commitment = new Commitment(protoCommitment.Content.Span);
        return commitment.VerifyRangeProof(protoCommitment.RangeProof.Span, label);
    }

    internal static bool VerifyPublication(this V1.Commitment commitment, V1.CommitmentPublication publication)
    {
        var cp = publication.ToModel();
        return commitment.Content.Span.SequenceEqual(cp.Commitment.C);
    }

    internal static Commitment ToModel(this V1.Commitment proto)
    {
        return new Commitment(proto.Content.Span);
    }

    internal static SecretCommitmentInfo ToModel(this V1.CommitmentPublication proto)
    {
        return new SecretCommitmentInfo(proto.Message, proto.RValue.Span);
    }

    internal static Guid ToModel(this Register.V1.Uuid allocationId) => Guid.Parse(allocationId.Value);

    internal static DateInterval ToModel(this V1.DateInterval proto)
    {
        return new DateInterval(
            proto.Start.ToDateTimeOffset(),
            proto.End.ToDateTimeOffset());
    }
}
