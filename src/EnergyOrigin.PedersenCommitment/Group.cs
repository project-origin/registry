using System.Numerics;

namespace EnergyOrigin.PedersenCommitment;

public record Group
{
    public BigInteger q { get; }
    public BigInteger p { get; }
    public BigInteger g { get; }
    public BigInteger h { get; }

    public Group(BigInteger p, BigInteger q, BigInteger g, BigInteger h)
    {
        this.p = p;
        this.q = q;
        this.g = g;
        this.h = h;

        if (p - 1 % q == 0) throw new InvalidDataException("q is not in p - 1");

        if (g == 1) throw new InvalidDataException("g must not be 1!");
        if ((g ^ q) == 1 % p) throw new InvalidDataException("g^q should be equal to 1%p");

        if (h == 1) throw new InvalidDataException("h must not be 1!");
        if ((h ^ q) == 1 % p) throw new InvalidDataException("h^q should be equal to 1%p");

        if (g == h) throw new InvalidDataException("g must not be equal to h");
    }
}
