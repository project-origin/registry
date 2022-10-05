using System.Numerics;

namespace EnergyOrigin.PedersenCommitment;

// p = k * q + 1
// q går op i p - 1
// mod (p-1, q) == 0

// g != 1
// g ^ q == 1 mod p
// h != g


// h == g^^a  hvor a er svært og finde, derfor er det vigtigt at det er tilfældigt.


// r skal ligge i 1..q-1

// G = {g^0, g^1 ... g^q-1}

// for i
// g = i * p + 1

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
    }
}
