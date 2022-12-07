using System.Numerics;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity;

public static class ProtoToModelExtensions
{

    internal static bool VerifyCommitment(this V1.Commitment commitment)
    {
        new Commitment(new BigInteger(commitment.Content.ToByteArray()), Group.Default);
        return commitment.RangeProof.IsEmpty;
    }

    internal static bool VerifyPublication(this V1.Commitment commitment, V1.CommitmentPublication publication)
    {
        var cp = new CommitmentParameters(publication.Message, new BigInteger(publication.RValue.ToByteArray()), Group.Default);
        return cp.Verify(new BigInteger(commitment.Content.ToByteArray()));
    }

    // public static SliceProof ToModel(this V1.SliceProof proto)
    // {
    //     return new SliceProof(
    //         proto.Source.ToModel(),
    //         proto.Quantity.ToModel(),
    //         proto.Remainder.ToModel());
    // }

    // public static Slice ToModel(this V1.Slice proto)
    // {
    //     return new Slice(
    //         proto.Source.ToModel(),
    //         proto.Quantity.ToModel(),
    //         proto.Remainder.ToModel(),
    //         new BigInteger(proto.ZeroR.ToByteArray()));
    // }

    internal static Commitment ToModel(this V1.Commitment proto)
    {
        return new Commitment(new BigInteger(proto.Content.ToByteArray()), Group.Default);
    }

    internal static CommitmentParameters ToModel(this V1.CommitmentPublication proto)
    {
        return new CommitmentParameters(proto.Message, new BigInteger(proto.RValue.ToByteArray()), Group.Default);
    }

    internal static Guid ToModel(this Register.V1.Uuid allocationId) => Guid.Parse(allocationId.Value);

    internal static DateInterval ToModel(this V1.DateInterval proto)
    {
        return new DateInterval(
            proto.Start.ToDateTimeOffset(),
            proto.End.ToDateTimeOffset());
    }
}
