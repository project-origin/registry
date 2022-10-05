using System.Numerics;

namespace EnergyOrigin.PedersenCommitment;

public record Commitment(BigInteger C, Group Group)
{
    public static Commitment Create(Group group, BigInteger m, params BigInteger[] r)
    {
        var rSum = BigInteger.Zero;
        foreach (var rEl in r)
        {
            rSum += rEl;
        }

        return new Commitment(BigInteger.ModPow(group.g, m, group.p) * BigInteger.ModPow(group.h, rSum, group.p) % group.p, group);
    }

    public static Commitment operator *(Commitment left, Commitment right)
    {
        if (left.Group != right.Group) throw new InvalidOperationException("Operator * between two commitments in different groups are not allowed");
        return new Commitment(left.C * right.C % left.Group.p, left.Group);
    }

    public static Commitment operator /(Commitment left, Commitment right)
    {
        if (left.Group != right.Group) throw new InvalidOperationException("Operator / between two commitments in different groups are not allowed");
        return new Commitment(left.C / right.C % left.Group.p, left.Group);
    }
}
