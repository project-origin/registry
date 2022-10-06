using System.Numerics;

namespace EnergyOrigin.PedersenCommitment;

public record Commitment
{
    public BigInteger C { get; }
    public Group Group { get; }

    public Commitment(BigInteger c, Group group)
    {
        //if (BigInteger.ModPow(c, group.q, group.p) != 1) throw new InvalidDataException("C^q should be equal to 1 mod p");

        C = c;
        Group = group;
    }

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
