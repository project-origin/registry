using System.Numerics;

namespace ProjectOrigin.PedersenCommitment;

public record Group
{
    private const int defaultBitLength = 200;

    public BigInteger p { get; }
    public BigInteger q { get; }
    public BigInteger g { get; }
    public BigInteger h { get; }

    private Random random;

    public Group(BigInteger p, BigInteger q, BigInteger g, BigInteger h, Random? random = null)
    {
        if (q.IsProbablyNotPrime()) throw new InvalidDataException("q is probably not a prime");
        if (p.IsProbablyNotPrime()) throw new InvalidDataException("p is probably not a prime");

        if ((p - 1) % q != 0) throw new InvalidDataException("q is not divisor in p - 1");

        if (g == 1) throw new InvalidDataException("g must not be 1!");

        if (BigInteger.ModPow(g, q, p) != 1) throw new InvalidDataException("g^q==1 mod p not satisfied.");

        if (h == 1) throw new InvalidDataException("h must not be 1!");
        if (BigInteger.ModPow(h, q, p) != 1) throw new InvalidDataException("h^q==1 mod p not satisfied.");

        if (g == h) throw new InvalidDataException("g must not be equal to h");

        this.q = q;
        this.p = p;
        this.g = g;
        this.h = h;
        this.random = random ?? Random.Shared;
    }

    public BigInteger RandomR()
    {
        return random.NextBigInteger(1, q);
    }

    public CommitmentParameters Commit(BigInteger m)
    {
        return new CommitmentParameters(m, RandomR(), this);
    }

    public static Group Create(int numberOfBits = defaultBitLength, Random? random = null)
    {
        random = random ?? Random.Shared;

        var q = GenerateQ(numberOfBits, random);
        var (p, k) = GenerateP(q);

        var g = GenerateGH(random, p, k);
        var h = GenerateGH(random, p, k, g);

        return new Group(p, q, g, h, random);
    }

    private static BigInteger GenerateQ(int numberOfBits, Random random)
    {
        BigInteger q;

        do
        {
            q = random.NextBigInteger(numberOfBits);
        }
        while (q.IsProbablyNotPrime());

        return q;
    }

    private static (BigInteger, int) GenerateP(BigInteger q)
    {
        BigInteger p;
        var k = 0;

        do
        {
            k += 1;
            p = k * q + 1;
        }
        while (p.IsProbablyNotPrime());

        return (p, k);
    }

    private static BigInteger GenerateGH(Random r, BigInteger p, int k, BigInteger? g = null)
    {
        BigInteger gh;
        do
        {
            var a = r.NextBigInteger(2, p - 1); // Select a (1 < a < p-1);
            gh = BigInteger.ModPow(a, k, p); // Compute g = a^k mod p (remember p = k*q + 1)
        }
        while (gh % p == 1 % p || gh == g); // If g == 1 mod p then goto 1 and try again. Otherwise return g.

        return gh;
    }
}
