using System.Runtime.InteropServices;
using System.Numerics;
namespace ProjectOrigin.PedersenCommitment;

using ProjectOrigin.PedersenCommitment.Ristretto;

public record Generator : IDisposable
{
    internal class Native
    {
        [DllImport("rust_ffi", EntryPoint = "pedersen_gens_default")]
        internal static extern IntPtr Default();

        [DllImport("rust_ffi", EntryPoint = "pedersen_gens_new")]
        internal static extern IntPtr New(IntPtr g, IntPtr h);

        [DllImport("rust_ffi", EntryPoint = "pedersen_gens_commit")]
        internal static extern IntPtr Commit(IntPtr self, byte[] m, byte[] r);

        [DllImport("rust_ffi", EntryPoint = "pedersen_gens_dispose")]
        internal static extern void Dispose(IntPtr self);
    }

    private IntPtr inner;

    public Generator()
    {
        this.inner = Native.Default();
    }

    public Generator(Point g, Point h)
    {
        this.inner = Native.New(g.ptr, h.ptr);
    }

    public void Dispose()
    {
        Native.Dispose(inner);
    }


    public Point Commit(BigInteger m, BigInteger r)
    {
        var ptr = Native.Commit(inner, Util.FromBigInteger(m), Util.FromBigInteger(r));
        return new Point(ptr);
    }


}


