namespace ProjectOrigin.PedersenCommitment.Ristretto;

/// <summary>
/// A sigma proof for proving a commitment is made to zero
/// </summary>
public class ZeroProof
{

    private Scalar c;
    private Scalar z;

    private ZeroProof(Scalar c, Scalar z)
    {
        this.c = c;
        this.z = z;
    }

    /// <summary>
    /// Prove a commitment made is to zero.
    /// Note that this does produce invalid proofs for invalid input.
    /// </summary>
    /// <param name="gen">Pedersen generator used</param>
    /// <param name="r">blinding/randomness used</param>
    /// <param name="label">domain seperating label</param>
    /// <returns>a new proof</returns>
    public static ZeroProof Prove(Generator gen, Scalar r, byte[] label)
    {
        var c0 = gen.H() * r;
        var oracle = (Point A) => Oracle(label, A, c0, gen.G(), gen.H());
        return ZeroProof.Prove(gen, r, oracle);
    }

    internal static ZeroProof Prove(Generator gen, Scalar r, Func<Point, Scalar> oracle)
    {
        var a = Scalar.Random();
        var A = gen.H() * a;
        var c = oracle(A);
        var z = a - c * r;
        return new ZeroProof(c, z);
    }

    /// <summary>
    /// Verify the proof
    /// </summary>
    /// <param name="gen">Pedersen generator used</param>
    /// <param name="c0">Commitment that should be zero</param>
    /// <param name="label">Domain seperating label</param>
    /// <returns>true if the commitment is made to zero and the proof holds</returns>
    public bool Verify(Generator gen, Point c0, byte[] label)
    {
        return this.Verify(gen, c0, (A) => Oracle(label, A, c0, gen.G(), gen.H()));
    }

    internal bool Verify(Generator gen, Point c0, Func<Point, Scalar> oracle)
    {
        var A = (gen.H() * this.z) + (c0 * this.c);
        var c = oracle(A);
        return this.c == c;
    }

    internal static Scalar Oracle(byte[] label, params Point[] inputs)
    {
        var m = (inputs.Length * Point.LENGTH) + label.Length;
        var digest = new byte[m];
        var begin = 0;
        foreach (var point in inputs)
        {
            var item = point.Compress()._bytes;
            System.Array.Copy(item, 0, digest, begin, item.Length);
            begin += item.Length;
        }
        System.Array.Copy(label, 0, digest, begin, label.Length);
        return Scalar.HashFromBytes(digest);
    }

    public byte[] Serialize()
    {
        var bytes = new byte[64];
        System.Array.Copy(c.ToBytes(), 0, bytes, 0, 32);
        System.Array.Copy(z.ToBytes(), 0, bytes, 32, 32);
        return bytes;
    }

    public static ZeroProof Deserialize(byte[] bytes)
    {
        if (bytes.Length != 64)
        {
            throw new ArgumentException("Bytes has to be 64 bytes long");
        }
        var c = new Scalar(bytes[0..32]);
        var z = new Scalar(bytes[32..64]);
        return new ZeroProof(c, z);
    }
}

public class EqualProof
{
    private ZeroProof proof;

    private EqualProof(ZeroProof proof)
    {
        this.proof = proof;
    }

    /// <summary>
    /// Prove two commitments are equal
    /// Note that this does produce invalid proofs for invalid input.
    /// </summary>
    /// <param name="gen">Pedersen generator used</param>
    /// <param name="r1">blinding/randomness used for first commitment</param>
    /// <param name="r2">blinding/randomness used for second commitment</param>
    /// <param name="label">domain seperating label</param>
    /// <returns>a new proof</returns>
    public static EqualProof Prove(Generator gen, Scalar r0, Scalar r1, Point c0, Point c1, byte[] label)
    {
        var oracle = (Point A) => ZeroProof.Oracle(label, A, c0, c1, gen.G(), gen.H());
        return new EqualProof(ZeroProof.Prove(gen, r0 - r1, oracle));
    }


    /// <summary>
    /// Verify the proof
    /// </summary>
    /// <param name="gen">Pedersen generator used</param>
    /// <param name="c0">First commitment</param>
    /// <param name="c1">Second commitment</param>
    /// <param name="label">Domain seperating label</param>
    /// <returns>true if the commitments are to the same value and the proof holds</returns>
    public bool Verify(Generator gen, Point c0, Point c1, byte[] label)
    {
        var oracle = (Point A) => ZeroProof.Oracle(label, A, c0, c1, gen.G(), gen.H());
        return proof.Verify(gen, c0 - c1, oracle);
    }

    public byte[] Serialize()
    {
        return proof.Serialize();
    }

    public static EqualProof Deserialize(byte[] bytes)
    {
        return new EqualProof(ZeroProof.Deserialize(bytes));
    }
}

public class SumProof
{
    private ZeroProof proof;

    private SumProof(ZeroProof proof)
    {
        this.proof = proof;
    }

    /// <summary>
    /// Prove two commitments are equal
    /// Note that this does produce invalid proofs for invalid input.
    /// </summary>
    /// <param name="gen">Pedersen generator used</param>
    /// <param name="label">domain seperating label</param>
    /// <param name="rsum">sum of <paramref name="rs"/></param>
    /// <param name="rs">series of blinding/randomness that is to sum to <paramref name="rsum"/></param>
    /// <returns>a new proof</returns>
    public static SumProof Prove(Generator gen, byte[] label, Scalar rsum, Point csum, params (Scalar, Point)[] vec)
    {
        var rs = new Scalar[vec.Length];
        for (int i = 0; i < vec.Length; i++)
            rs[i] = vec[i].Item1;
        var rsum2 = Scalar.Sum(rs);


        var oracle = (Point A) =>
        {
            var n = vec.Length;
            var args = new Point[n + 3];
            for (int i = 0; i < n; i++)
                args[i] = vec[i].Item2;
            args[n] = gen.G();
            args[n + 1] = gen.H();
            args[n + 2] = A;
            return ZeroProof.Oracle(label, args);
        };
        return new SumProof(ZeroProof.Prove(gen, rsum - rsum2, oracle));
    }

    /// <summary>
    /// Verify the proof
    /// </summary>
    /// <param name="gen">Pedersen generator used</param>
    /// <param name="csum">Commitment made to the sum</param>
    /// <param name="cs">Series of commitments made to the parts of the sum</param>
    /// <param name="label">Domain seperating label</param>
    /// <returns>true of the commitments are made to the commitment sum</returns>
    public bool Verify(Generator gen, byte[] label, Point csum, params Point[] cs)
    {
        var csum2 = Point.Sum(cs);

        var oracle = (Point A) =>
        {
            var n = cs.Length;
            var args = new Point[n + 3];
            System.Array.Copy(cs, 0, args, 0, n);
            args[n] = gen.G();
            args[n + 1] = gen.H();
            args[n + 2] = A;
            return ZeroProof.Oracle(label, args);
        };
        return proof.Verify(gen, csum - csum2, oracle);
    }

    public byte[] Serialize()
    {
        return proof.Serialize();
    }

    public static SumProof Deserialize(byte[] bytes)
    {
        return new SumProof(ZeroProof.Deserialize(bytes));
    }
}
