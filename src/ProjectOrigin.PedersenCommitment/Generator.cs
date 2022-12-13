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

    public void Dispose()
    {
        Native.Dispose(inner);
    }


    public Point Commit(BigInteger m, BigInteger r)
    {
        var ptr = Native.Commit(inner, FromBigInteger(m), FromBigInteger(r));
        return new Point(ptr);
    }


    private static byte[] FromBigInteger(BigInteger b)
    {
        var count = b.GetByteCount(true);
        if (count > 32) {
            throw new ArgumentException("BigInteger too large, above 32 bytes");
        }

        var bytes = b.ToByteArray(true, false);
        var outs = new byte[32];
        Array.Copy(bytes, outs, count);
        return outs;
    }
}


