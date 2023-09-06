using System;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Extensions;

public static class ProtoToModelExtensions
{
    public static bool VerifyCommitment(this V1.Commitment protoCommitment, string label)
    {
        var commitment = new Commitment(protoCommitment.Content.Span);
        return commitment.VerifyRangeProof(protoCommitment.RangeProof.Span, label);
    }

    public static bool VerifyPublication(this V1.Commitment commitment, V1.CommitmentPublication publication)
    {
        var cp = publication.ToModel();
        return commitment.Content.Span.SequenceEqual(cp.Commitment.C);
    }

    public static Commitment ToModel(this V1.Commitment proto)
    {
        return new Commitment(proto.Content.Span);
    }

    public static SecretCommitmentInfo ToModel(this V1.CommitmentPublication proto)
    {
        return new SecretCommitmentInfo(proto.Message, proto.RValue.Span);
    }

    public static Guid ToModel(this Common.V1.Uuid allocationId) => Guid.Parse(allocationId.Value);
}
