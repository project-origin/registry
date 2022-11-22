using System.Numerics;
using ProjectOrigin.Electricity.Models;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Electricity;

public static class ProtoToModelExtensions
{
    public static SliceProof ToModel(this V1.SliceProof proto)
    {
        return new SliceProof(
            proto.Source.ToModel(),
            proto.Quantity.ToModel(),
            proto.Remainder.ToModel());
    }

    public static Slice ToModel(this V1.Slice proto)
    {
        return new Slice(
            proto.Source.ToModel(),
            proto.Quantity.ToModel(),
            proto.Remainder.ToModel(),
            new BigInteger(proto.ZeroR.ToByteArray()));
    }

    public static Commitment ToModel(this V1.Commitment proto)
    {
        return new Commitment(new BigInteger(proto.C.ToByteArray()), Group.Default);
    }

    public static CommitmentParameters ToModel(this V1.CommitmentProof proto)
    {
        return new CommitmentParameters(proto.M, new BigInteger(proto.R.ToByteArray()), Group.Default);
    }

    public static Guid ToModel(this Register.V1.Uuid allocationId) => Guid.Parse(allocationId.Value);

    public static TimePeriod ToModel(this V1.TimePeriod proto)
    {
        return new TimePeriod(
            proto.DateTimeFrom.ToDateTimeOffset(),
            proto.DateTimeTo.ToDateTimeOffset());
    }

    public static FederatedStreamId ToModel(this Register.V1.FederatedStreamId proto)
    {
        return new FederatedStreamId(proto.Registry, Guid.Parse(proto.StreamId.Value));
    }

}
