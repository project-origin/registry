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

    private BigInteger p { get; }
    private BigInteger q { get; }
    private BigInteger g { get; }
    private BigInteger h { get; }
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

    public CommitmentParameters Commit(BigInteger m)
    {
        return new CommitmentParameters(m, RandomR(), this);
    }

    public Commitment CreateCommitment(BigInteger c)
    {
        if (BigInteger.ModPow(c, q, p) != 1) throw new InvalidDataException("C^q should be equal to 1 mod p");

        return new Commitment(c, this);
    }

    public CommitmentParameters CreateParameters(BigInteger message, BigInteger r)
    {
        return new CommitmentParameters(message, r, this);
    }

    public BigInteger RandomR()
    {
        return _random.NextBigInteger(1, q); //Probably redo TODO!!!
    }

    public Commitment CreateCommitment(BigInteger message, BigInteger rValue)
    {
        var c = BigInteger.ModPow(g, message, p) * BigInteger.ModPow(h, rValue, p) % p; //Probably redo TODO!!!

        return CreateCommitment(c);
    }

    public Commitment CreateZeroCommitment(CommitmentParameters left, params CommitmentParameters[] right)
    {
        var mSum = left.Message - right.Select(x => x.Message).Aggregate((a, b) => a + b);
        if (mSum != 0)
        {
            throw new NotSupportedException("Sum of messages are not zero.");
        }
        var rSum = (left.RValue - right.Select(x => x.RValue).Aggregate((a, b) => a + b)).MathMod(q);

        return CreateCommitment(0, rSum);
    }

    public ReadOnlySpan<byte> CreateEqualityProof(CommitmentParameters sourceParams, params CommitmentParameters[] fs)
    {
        return ReadOnlySpan<byte>.Empty; //TODO
    }

    public bool VerifyEqualityProof(ReadOnlySpan<byte> bytes, Commitment commitment1, Commitment commitment2)
    {
        return bytes.Length == 0; //TODO
    }

    internal ReadOnlySpan<byte> CreateRangeProof(BigInteger message, BigInteger rValue)
    {
        return ReadOnlySpan<byte>.Empty; //TODO!!!
    }

    public bool VerifyRangeProof(ReadOnlySpan<byte> bytes, Commitment commitment)
    {
        return bytes.Length == 0; //TODO
    }

    internal Commitment Product(BigInteger cLeft, BigInteger cRight)
    {
        return CreateCommitment((cLeft * cRight) % p);
    }

    internal Commitment InverseProduct(BigInteger cLeft, BigInteger cRight)
    {
        var theInverse = BigInteger.ModPow(cRight, q - 1, p);
        return CreateCommitment(cLeft * theInverse % p);
    }

    #region GenerateGroup

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

    #endregion
}
