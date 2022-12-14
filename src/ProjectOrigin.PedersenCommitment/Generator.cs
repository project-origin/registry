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
        internal static extern IntPtr Commit(IntPtr self, IntPtr m, IntPtr r);

        [DllImport("rust_ffi", EntryPoint = "pedersen_gens_commit_bytes")]
        internal static extern IntPtr Commit(IntPtr self, byte[] m, byte[] r);
        [DllImport("rust_ffi", EntryPoint = "pedersen_gens_dispose")]
        internal static extern void Dispose(IntPtr self);
    }

    internal IntPtr ptr;

    public Generator()
    {
        this.ptr = Native.Default();
    }

    public Generator(Point g, Point h)
    {
        this.ptr = Native.New(g.ptr, h.ptr);
    }

    public void Dispose()
    {
        Native.Dispose(ptr);
    }


    public Point Commit(BigInteger m, BigInteger r)
    {
        var ptr = Native.Commit(this.ptr, Util.FromBigInteger(m), Util.FromBigInteger(r));
        return new Point(ptr);
    }

    public Point Commit(ulong m, ulong r)
    {
        var ptr = Native.Commit(this.ptr, new Scalar(m).ptr, new Scalar(r).ptr);
        return new Point(ptr);
    }

}


