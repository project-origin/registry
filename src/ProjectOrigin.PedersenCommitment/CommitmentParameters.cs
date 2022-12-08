using System.Numerics;

namespace ProjectOrigin.PedersenCommitment;

public record CommitmentParameters
{
    public BigInteger Message { get; }

    public BigInteger RValue { get; }

    public Commitment Commitment { get => _group.CreateCommitment(Message, RValue); }

    public BigInteger C { get => Commitment.C; }

    public ReadOnlySpan<byte> RangeProof { get => _group.CreateRangeProof(Message, RValue); }

    private Group _group;

    internal CommitmentParameters(BigInteger m, BigInteger r, Group group)
    {
        Message = m;
        RValue = r;
        _group = group;
    }
}
