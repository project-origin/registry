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
        var a = Scalar.Random();
        var A = gen.H() * a;
        var c = Oracle(label, A, gen.G(), gen.H());
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
        var A = (gen.H() * this.z) + (c0 * this.c);
        var c = Oracle(label, A, gen.G(), gen.H());
        return this.c == c;
    }

    private static Scalar Oracle(byte[] label, params Point[] inputs)
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
    public static EqualProof Prove(Generator gen, Scalar r0, Scalar r1, byte[] label)
    {
        return new EqualProof(ZeroProof.Prove(gen, r0 - r1, label));
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
        return proof.Verify(gen, c0 - c1, label);
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
    private EqualProof proof;

    private SumProof(EqualProof proof)
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
    public static SumProof Prove(Generator gen, byte[] label, Scalar rsum, params Scalar[] rs)
    {
        var rsum2 = rs[0];
        foreach (Scalar r in rs[1..])
        { // TODO: Probably use a Native function for speedup
            rsum2 += r;
        }
        return new SumProof(EqualProof.Prove(gen, rsum, rsum2, label));
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
        var csum2 = cs[0];
        foreach (Point r in cs[1..])
        { // TODO: Probably use a Native function for speedup
            csum2 += r;
        }
        return proof.Verify(gen, csum, csum2, label);
    }

    public byte[] Serialize()
    {
        return proof.Serialize();
    }

    public static SumProof Deserialize(byte[] bytes)
    {
        return new SumProof(EqualProof.Deserialize(bytes));
    }
}
