using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ProjectOrigin.PedersenCommitment.Ristretto;

public sealed class Point
{
    private class Native
    {
        [DllImport("rust_ffi", EntryPoint = "ristretto_point_from_uniform_bytes")]
        internal static extern IntPtr FromUniformBytes(byte[] bytes);

        // TODO: check if byte[] is a sane argument
        [DllImport("rust_ffi", EntryPoint = "ristretto_point_compress")]
        internal static extern void Compress(IntPtr self, byte[] bytes_ptr);

        // TODO: check if byte[] is a sane argument
        [DllImport("rust_ffi", EntryPoint = "ristretto_point_decompress")]
        internal static extern IntPtr Decompress(byte[] bytes);

        [DllImport("rust_ffi", EntryPoint = "ristretto_point_free")]
        internal static extern void Free(IntPtr self);

        [DllImport("rust_ffi", EntryPoint = "ristretto_point_add")]
        internal static extern IntPtr Add(IntPtr lhs, IntPtr rhs);

        [DllImport("rust_ffi", EntryPoint = "ristretto_point_sub")]
        internal static extern IntPtr Sub(IntPtr lhs, IntPtr rhs);

        [DllImport("rust_ffi", EntryPoint = "ristretto_point_negate")]
        internal static extern IntPtr Negate(IntPtr self);

        [DllImport("rust_ffi", EntryPoint = "ristretto_point_mul_bytes")]
        internal static extern IntPtr Mul(IntPtr lhs, byte[] rhs);

        [DllImport("rust_ffi", EntryPoint = "ristretto_point_mul_scalar")]
        internal static extern IntPtr Mul(IntPtr point, IntPtr scalar);

        [DllImport("rust_ffi", EntryPoint = "ristretto_point_equals")]
        internal static extern bool Equals(IntPtr lhs, IntPtr rhs);

        [DllImport("rust_ffi", EntryPoint = "ristretto_point_gut_spill")]
        internal static extern void GutSpill(IntPtr self);
    }

    internal readonly IntPtr _ptr;

    internal Point(IntPtr ptr)
    {
        _ptr = ptr;
    }

    ~Point()
    {
        Native.Free(_ptr);
    }

    public static Point FromUniformBytes(byte[] bytes)
    {
        // TODO: Ensure length of bytes is appropriate
        return new Point(Native.FromUniformBytes(bytes));
    }

    public CompressedPoint Compress()
    {
        var bytes = new byte[32]; // allocate bytes
        Native.Compress(_ptr, bytes);
        return new CompressedPoint(bytes);
    }

    public static Point operator +(Point left, Point right)
    {
        var ptr = Native.Add(left._ptr, right._ptr);
        return new Point(ptr);
    }

    public static Point operator -(Point left, Point right)
    {
        var ptr = Native.Sub(left._ptr, right._ptr);
        return new Point(ptr);
    }


    public static Point operator -(Point self)
    {
        var ptr = Native.Negate(self._ptr);
        return new Point(ptr);
    }

    public static Point operator *(Point left, Scalar right)
    {
        return new Point(Native.Mul(left._ptr, right._ptr));
    }

    public static Point operator *(Scalar left, Point right)
    {
        return new Point(Native.Mul(right._ptr, left._ptr));
    }

    public override bool Equals(object? obj)
    {
        if (obj is Point)
        {
            return this == (Point)obj;
        }
        else
        {
            return false;
        }
    }

    public static bool operator ==(Point left, Point right)
    {
        if (left._ptr == right._ptr)
        {
            return true;
        }
        return Native.Equals(left._ptr, right._ptr);
    }

    public static bool operator !=(Point left, Point right)
    {
        return !Native.Equals(left._ptr, right._ptr);
    }

    public void GutSpill()
    {
        Native.GutSpill(_ptr);
    }

    public override int GetHashCode() => base.GetHashCode();
    internal static Point Decompress(byte[] bytes)
    {
        var ptr = Native.Decompress(bytes);
        if (ptr == IntPtr.Zero)
        { // null pointer == could not decompress
            throw new ArgumentException("Could not decompress RistrettoPoint");
        }
        return new Point(ptr);
    }
}

public readonly struct CompressedPoint
{

    internal readonly byte[] _bytes;

    public CompressedPoint(byte[] bytes)
    {
        if (bytes.Length != 32)
        {
            throw new ArgumentException("Byte array must be 32 long");
        }
        _bytes = bytes;
    }


    [DllImport("rust_ffi", EntryPoint = "compressed_ristretto_from_bytes")]
    internal static extern IntPtr FromBytes(byte[] bytes);

    [DllImport("rust_ffi", EntryPoint = "compressed_ristretto_to_bytes")]
    internal static extern void ToBytes(IntPtr self, byte[] bytes);

    public Point Decompress()
    {
        return Point.Decompress(_bytes);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is not CompressedPoint)
        {
            return false;
        }
        var other = (CompressedPoint)obj;
        return _bytes.SequenceEqual(other._bytes);
    }



    public override int GetHashCode() => base.GetHashCode();

}
