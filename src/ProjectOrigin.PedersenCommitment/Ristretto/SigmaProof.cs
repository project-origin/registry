namespace ProjectOrigin.PedersenCommitment.Ristretto;

public class ZeroProof
{

    private Scalar c;
    private Scalar z;

    private ZeroProof(Scalar c, Scalar z)
    {
        this.c = c;
        this.z = z;
    }

    public static ZeroProof Prove(Generator gen, Scalar r, byte[] label)
    {
        var a = Scalar.Random();
        var c = Oracle(gen.H() * a, label);
        var z = a - c * r;
        return new ZeroProof(c, z);
    }

    public bool Verify(Generator gen, Point sum, byte[] label)
    {
        var A = (gen.H() * this.z) + (sum * this.c);
        var c = Oracle(A, label);
        return this.c == c;
    }

    private static Scalar Oracle(Point A, byte[] label)
    {
        var compressed = A.Compress();
        var n = compressed._bytes.Length;
        var m = label.Length;

        var digest = new byte[n+m];
        System.Array.Copy(compressed._bytes, 0, digest, 0, n);
        System.Array.Copy(label, 0, digest, n, m);
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

    public static EqualProof Prove(Generator gen, Scalar r0, Scalar r1, byte[] label)
    {
        return new EqualProof(ZeroProof.Prove(gen, r0 - r1, label));
    }


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
