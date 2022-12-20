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

    public static ZeroProof Prove(Generator gen, Scalar r)
    {
        var a = Scalar.Random();
        var c = Oracle(gen.H() * a);
        var z = a - c * r;
        return new ZeroProof(c, z);
    }

    public bool Verify(Generator gen, Point sum)
    {
        var A = (gen.H() * this.z) + (sum * this.c);
        var c = Oracle(A);
        return this.c == c;
    }

    private static Scalar Oracle(Point A) // TODO: Add label and other inputs
    {
        var compressed = A.Compress();
        return Scalar.HashFromBytes(compressed._bytes);
    }
}
