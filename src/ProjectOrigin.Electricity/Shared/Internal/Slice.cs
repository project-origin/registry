using System.Numerics;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Shared.Internal;

public record Slice(Commitment Source, Commitment Quantity, Commitment Remainder, BigInteger ZeroR)
{
    public static Slice From(V1.Slice proto)
    {
        return new Slice(
            Mapper.ToModel(proto.Source),
            Mapper.ToModel(proto.Quantity),
            Mapper.ToModel(proto.Remainder),
            new BigInteger(proto.ZeroR.ToByteArray()));
    }
}
