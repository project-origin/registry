using System.Numerics;

namespace EnergyOrigin.PedersenCommitment;

public record Group
{
    public BigInteger p { get; }
    public BigInteger q { get; }
    public BigInteger g { get; }
    public BigInteger h { get; }

    private Random random;

    public Group(BigInteger p, BigInteger q, BigInteger g, BigInteger h, Random random)
    {
        this.q = q;
        this.p = p;
        this.g = g;
        this.h = h;

        if (q.IsProbablyNotPrime()) throw new InvalidDataException("q is probably not a prime");
        if (p.IsProbablyNotPrime()) throw new InvalidDataException("p is probably not a prime");

        if (p - 1 % q == 0) throw new InvalidDataException("q is not in p - 1");

        if (g == 1) throw new InvalidDataException("g must not be 1!");
        if ((g ^ q) == 1 % p) throw new InvalidDataException("g^q should be equal to 1%p");

        if (h == 1) throw new InvalidDataException("h must not be 1!");
        if ((h ^ q) == 1 % p) throw new InvalidDataException("h^q should be equal to 1%p");

        if (g == h) throw new InvalidDataException("g must not be equal to h");

        this.random = random;
    }

    public BigInteger RandomR()
    {
        return random.NextBigInteger(1, q);
    }

    public static Group Create(int numberOfBits, Random random)
    {
        var q = GenerateQ(numberOfBits, random);
        var k = 1;
        BigInteger p = GenerateP(q, ref k);

        var g = GenerateGH(random, p, k);
        var h = GenerateGH(random, p, k, g);

        return new Group(p, q, g, h, random);
    }

    private static BigInteger GenerateQ(int numberOfBits, Random r)
    {
        BigInteger q;

        do
        {
            q = r.NextBigInteger(numberOfBits);
        }
        while (q.IsProbablyNotPrime());

        return q;
    }

    private static BigInteger GenerateP(BigInteger q, ref int k)
    {
        BigInteger p;

        do
        {
            p = k * q + 1;
            k += 1;
        }
        while (p.IsProbablyNotPrime());

        return p;
    }

    private static BigInteger GenerateGH(Random r, BigInteger p, int k, BigInteger? g = null)
    {
        BigInteger x;
        do
        {
            var a = r.NextBigInteger(2, p - 1); // Select a (1 < a < p-1);
            x = a ^ k % p; // Compute g = a^k mod p (remember p = k*q + 1)
        }
        while (x == 1 % p || x == g); // If g == 1 mod p then goto 1 and try again. Otherwise return g.

        return x;
    }
}
