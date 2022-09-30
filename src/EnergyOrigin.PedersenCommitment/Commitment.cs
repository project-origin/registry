using System.Numerics;

namespace EnergyOrigin.PedersenCommitment;

public record Commitment(BigInteger C, CurveParams qgh)
{
    public static Commitment Create(CurveParams cp, BigInteger m, params BigInteger[] r)
    {
        var rSum = BigInteger.Zero;
        foreach (var rEl in r)
        {
            rSum += rEl;
        }

        return new Commitment(BigInteger.ModPow(cp.g, m, cp.q) * BigInteger.ModPow(cp.h, rSum, cp.q) % cp.q, cp);
    }

    public static Commitment operator *(Commitment left, Commitment right)
    {
        return new Commitment(left.C * right.C % left.qgh.q, left.qgh);
    }

    public static Commitment operator /(Commitment left, Commitment right)
    {
        return new Commitment(left.C / right.C % left.qgh.q, left.qgh);
    }
}
