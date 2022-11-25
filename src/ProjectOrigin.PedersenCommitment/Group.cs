using System.Numerics;

namespace ProjectOrigin.PedersenCommitment;

public record Group
{
    private const int DefaultBitLength = 200;

    private static Lazy<Group> _lazyDefault = new Lazy<Group>(() => new Group(
        p: BigInteger.Parse("519410415765480562065563560862184550988245350627770327636130577"),
        q: BigInteger.Parse("1202338925383056856633248983477279053213530904230949832491043"),
        g: BigInteger.Parse("101455240839796123327081946568988620614409829310312504112082811"),
        h: BigInteger.Parse("162315825204305527697219690878071619973299472069112727941372177")
    ), true);

    public static Group Default
    {
        get
        {
            return _lazyDefault.Value;
        }
    }

    public BigInteger p { get; }
    public BigInteger q { get; }
    public BigInteger g { get; }
    public BigInteger h { get; }

    private Random _random;

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
        _random = random ?? Random.Shared;
    }

    public BigInteger RandomR()
    {
        return _random.NextBigInteger(1, q);
    }

    public CommitmentParameters Commit(BigInteger m)
    {
        return new CommitmentParameters(m, RandomR(), this);
    }

    public static Group Create(int numberOfBits = DefaultBitLength, Random? random = null)
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
