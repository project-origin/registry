using System.Runtime.InteropServices;
using System.Numerics;

namespace ProjectOrigin.PedersenCommitment.Ristretto;

/**
 * @brief Scalar referencing a Rust object on the heap. Guaranteed to always be in the field.
 */
public class Scalar : IDisposable
{
    internal class Native
    {
        [DllImport("rust_ffi", EntryPoint = "scalar_new")]
        internal static extern IntPtr New(byte[] bytes);

        [DllImport("rust_ffi", EntryPoint = "scalar_random")]
        internal static extern IntPtr Random();

        [DllImport("rust_ffi", EntryPoint = "scalar_to_bytes")]
        internal static extern void ToBytes(IntPtr self, byte[] output);

        [DllImport("rust_ffi", EntryPoint = "scalar_free")]
        internal static extern void Dispose(IntPtr self);

        [DllImport("rust_ffi", EntryPoint = "scalar_add")]
        internal static extern IntPtr Add(IntPtr lhs, IntPtr rhs);

        [DllImport("rust_ffi", EntryPoint = "scalar_mul")]
        internal static extern IntPtr Mul(IntPtr lhs, byte[] rhs);

        [DllImport("rust_ffi", EntryPoint = "scalar_equals")]
        internal static extern bool Equals(IntPtr lhs, IntPtr rhs);
    }

    internal readonly IntPtr ptr;

    internal Scalar(IntPtr ptr)
    {
        this.ptr = ptr;
    }

    public Scalar(BigInteger bigInteger)
    {
        var bytes = Util.FromBigInteger(bigInteger);
        this.ptr = Native.New(bytes);
    }

    public Scalar(byte[] bytes)
    {
        this.ptr = Native.New(bytes);
    }


    public byte[] ToBytes()
    {
        var bytes = new byte[32];
        Native.ToBytes(ptr, bytes);
        return bytes;
    }


    public void Dispose()
    {
        Native.Dispose(ptr);
    }

    public static Scalar operator +(Scalar left, Scalar right)
    {
        var ptr = Native.Add(left.ptr, right.ptr);
        return new Scalar(ptr);
    }

    public static Scalar operator *(Scalar left, BigInteger right)
    {
        var ptr = Native.Mul(left.ptr, Util.FromBigInteger(right));
        return new Scalar(ptr);

    }

    public static Scalar operator *(BigInteger left, Scalar right)
    {
        var ptr = Native.Mul(right.ptr, Util.FromBigInteger(left));
        return new Scalar(ptr);

    }

    public override bool Equals(object? obj)
    {
        if (obj is Scalar) {
            return this == (Scalar) obj;
        } else {
            return false;
        }
    }

    public static bool operator ==(Scalar left, Scalar right)
    {
        if (left.ptr == right.ptr) {
            return true;
        }
        return Native.Equals(left.ptr, right.ptr);
    }

    public static bool operator !=(Scalar left, Scalar right)
    {
        return !Native.Equals(left.ptr, right.ptr);
    }


    public override int GetHashCode() => base.GetHashCode();
}
