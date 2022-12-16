using System.Numerics;

namespace ProjectOrigin.PedersenCommitment;

public record Commitment
{
    public BigInteger C { get; }

    private Group _group;

    internal Commitment(BigInteger c, Group group)
    {
        C = c;
        _group = group;
    }

    public static Commitment operator *(Commitment left, Commitment right)
    {
        if (left._group != right._group) throw new InvalidOperationException("Operator * between two commitments in different groups are not allowed");
        return left._group.Product(left.C, right.C);
    }

    public static Commitment operator /(Commitment left, Commitment right)
    {
        if (left._group != right._group) throw new InvalidOperationException("Operator / between two commitments in different groups are not allowed");
        return left._group.InverseProduct(left.C, right.C);
    }
}
