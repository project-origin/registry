using System.Numerics;
using System.Runtime.Serialization;

namespace ProjectOrigin.PedersenCommitment;

public record CommitmentParameters
{
    public BigInteger m { get; }

    public BigInteger r { get; }

    [IgnoreDataMember]
    public Group Group { get; }

    [IgnoreDataMember]
    public Commitment Commitment { get => Commitment.Create(Group, m, r); }

    [IgnoreDataMember]
    public BigInteger C { get => Commitment.C; }

    [IgnoreDataMember]
    public ReadOnlySpan<byte> RangeProof { get => ReadOnlySpan<byte>.Empty; } //TODO!!!

    public CommitmentParameters(BigInteger m, BigInteger r, Group group)
    {
        this.m = m;
        this.r = r;
        Group = group;
    }

    public bool Verify(BigInteger c)
    {
        var cActual = new Commitment(c, Group);
        return Verify(cActual);
    }

    public bool Verify(Commitment cActual)
    {
        var cExpected = Commitment.Create(Group, m, r);

        return cActual == cExpected;
    }
}
