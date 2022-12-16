using System.Runtime.InteropServices;

namespace ProjectOrigin.PedersenCommitment.Ristretto;

/**
 * @brief Scalar referencing a Rust object on the heap. Guaranteed to always be in the field.
 */
public sealed class Scalar : IDisposable
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
        internal static extern void Free(IntPtr self);

        [DllImport("rust_ffi", EntryPoint = "scalar_add")]
        internal static extern IntPtr Add(IntPtr lhs, IntPtr rhs);

        [DllImport("rust_ffi", EntryPoint = "scalar_sub")]
        internal static extern IntPtr Sub(IntPtr lhs, IntPtr rhs);

        [DllImport("rust_ffi", EntryPoint = "scalar_negate")]
        internal static extern IntPtr Negate(IntPtr self);

        [DllImport("rust_ffi", EntryPoint = "scalar_mul")]
        internal static extern IntPtr Mul(IntPtr lhs, byte[] rhs);

        [DllImport("rust_ffi", EntryPoint = "scalar_equals")]
        internal static extern bool Equals(IntPtr lhs, IntPtr rhs);

        [DllImport("rust_ffi", EntryPoint = "scalar_hash_from_bytes")]
        internal static extern IntPtr HashFromBytes(byte[] bytes, int len);
    }

    internal readonly IntPtr _ptr;

    internal Scalar(IntPtr ptr)
    {
        _ptr = ptr;
    }

    ~Scalar()
    {
        Native.Free(_ptr);
    }

    public Scalar(ulong value)
    {
        var bytes = value.ToByteArray(32);
        _ptr = Native.New(bytes);
    }

    public Scalar(ReadOnlySpan<byte> bytes)
    {
        _ptr = Native.New(bytes.ToArray());
    }

    public Scalar(byte[] bytes)
    {
        if (bytes.Length != 32)
        {
            throw new ArgumentException("Byte length has to 32");
        }
        _ptr = Native.New(bytes);
    }

    public static Scalar Random()
    {
        return new Scalar(Native.Random());
    }

    public static Scalar HashFromBytes(byte[] bytes)
    {
        return new Scalar(Native.HashFromBytes(bytes, bytes.Length));
    }

    public byte[] ToBytes()
    {
        var bytes = new byte[32];
        Native.ToBytes(_ptr, bytes);
        return bytes;
    }

    public void Dispose()
    {
        Native.Free(_ptr);
    }

    public static Scalar operator +(Scalar left, Scalar right)
    {
        var ptr = Native.Add(left._ptr, right._ptr);
        return new Scalar(ptr);
    }

    public static Scalar operator -(Scalar left, Scalar right)
    {
        var ptr = Native.Sub(left._ptr, right._ptr);
        return new Scalar(ptr);
    }

    public static Scalar operator -(Scalar self)
    {
        var ptr = Native.Negate(self._ptr);
        return new Scalar(ptr);
    }

    public override bool Equals(object? obj)
    {
        if (obj is Scalar)
        {
            return this == (Scalar)obj;
        }
        else
        {
            return false;
        }
    }

    public static bool operator ==(Scalar left, Scalar right)
    {
        if (left._ptr == right._ptr)
        {
            return true;
        }
        return Native.Equals(left._ptr, right._ptr);
    }

    public static bool operator !=(Scalar left, Scalar right)
    {
        return !Native.Equals(left._ptr, right._ptr);
    }


    public override int GetHashCode() => base.GetHashCode();
}
