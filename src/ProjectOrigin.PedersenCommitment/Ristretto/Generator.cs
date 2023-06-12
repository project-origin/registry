using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using ProjectOrigin.PedersenCommitment.Ristretto;

namespace ProjectOrigin.PedersenCommitment;

public sealed record Generator
{
    private class Native
    {
        [DllImport("rust_ffi", EntryPoint = "pedersen_gens_default")]
        internal static extern IntPtr Default();

        [DllImport("rust_ffi", EntryPoint = "pedersen_gens_new")]
        internal static extern IntPtr New(IntPtr g, IntPtr h);

        [DllImport("rust_ffi", EntryPoint = "pedersen_gens_commit")]
        internal static extern IntPtr Commit(IntPtr self, IntPtr m, IntPtr r);

        [DllImport("rust_ffi", EntryPoint = "pedersen_gens_commit_bytes")]
        internal static extern IntPtr Commit(IntPtr self, byte[] m, byte[] r);

        [DllImport("rust_ffi", EntryPoint = "pedersen_gens_free")]
        internal static extern void Free(IntPtr self);

        [DllImport("rust_ffi", EntryPoint = "pedersen_gens_B")]
        internal static extern IntPtr G(IntPtr self);

        [DllImport("rust_ffi", EntryPoint = "pedersen_gens_B_blinding")]
        internal static extern IntPtr H(IntPtr self);
    }

    public static Lazy<Generator> LazyGenerator = new Lazy<Generator>(() =>
    {
        // We use pi with 42 digits as the seed, because, well 42 is the answer to everything.
        var piBytes = Encoding.ASCII.GetBytes("3.141592653589793238462643383279502884197169");
        var sha1 = SHA512.HashData(piBytes);
        var sha2 = SHA512.HashData(sha1);

        var g1 = Point.FromUniformBytes(sha1);
        var g2 = Point.FromUniformBytes(sha2);

        return new Generator(g1, g2);
    }, true);

    public static Generator Default
    {
        get => LazyGenerator.Value;
    }

    internal IntPtr _ptr;

    /// <summary>
    /// Construct a new generator
    /// </summary>
    /// <param name="g">Generator for mapping the messages to the curve</param>
    /// <param name="h">Generator for mapping the blinding to the curve</param>
    /// <returns>A new generator from the points</returns>
    public Generator(Point g, Point h)
    {
        _ptr = Native.New(g._ptr, h._ptr);
    }

    ~Generator()
    {
        Native.Free(_ptr);
    }

    /// <summary>
    /// The 'G' generator of the group mapping the messages
    /// </summary>
    /// <returns>The 'G' generator as a Ristretto Point</returns>
    public Point G()
    {
        return new Point(Native.G(this._ptr));
    }

    /// <summary>
    /// The 'H' generator of the group mapping the messages
    /// </summary>
    /// <returns>The 'H' generator as a Ristretto Point</returns>
    public Point H()
    {
        return new Point(Native.H(this._ptr));
    }

    /// <summary>
    /// Constructs a new Pedersen Commitment
    /// </summary>
    /// <param name="m">The message</param>
    /// <param name="r">The blinding/randomness</param>
    /// <returns>A new Ristretto Point representing the commitment</returns>
    public Point Commit(ulong m, ulong r)
    {
        var ptr = Native.Commit(_ptr, new Scalar(m)._ptr, new Scalar(r)._ptr);
        return new Point(ptr);
    }

    /// <summary>
    /// Constructs a new Pedersen Commitment
    /// </summary>
    /// <param name="m">The message</param>
    /// <param name="r">The blinding/randomness</param>
    /// <returns>A new Ristretto Point representing the commitment</returns>
    public Point Commit(ulong m, Scalar r)
    {
        var ptr = Native.Commit(_ptr, new Scalar(m)._ptr, r._ptr);
        return new Point(ptr);
    }
}
