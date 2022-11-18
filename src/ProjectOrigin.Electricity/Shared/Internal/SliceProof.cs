
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Shared.Internal;

public record SliceProof(CommitmentParameters Source, CommitmentParameters Quantity, CommitmentParameters Remainder)
{
    public static implicit operator SliceProof(V1.SliceProof proto)
    {
        return new SliceProof(
            Mapper.ToModel(proto.Source),
            Mapper.ToModel(proto.Quantity),
            Mapper.ToModel(proto.Remainder));
    }
}


