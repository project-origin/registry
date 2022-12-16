using System.Runtime.InteropServices;
using System.Numerics;
using System.Diagnostics.CodeAnalysis;

namespace ProjectOrigin.PedersenCommitment.Ristretto;

internal class NativePoint {
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

public sealed class Point
{

    internal readonly IntPtr ptr;

    internal Point(IntPtr ptr)
    {
        this.ptr = ptr;
    }

    ~Point()
    {
        NativePoint.Free(ptr);
    }

    public static Point FromUniformBytes(byte[] bytes)
    {
        // TODO: Ensure length of bytes is appropriate
        return new Point(NativePoint.FromUniformBytes(bytes));
    }

    public CompressedPoint Compress()
    {
        var bytes = new byte[32]; // allocate bytes
        NativePoint.Compress(ptr, bytes);
        return new CompressedPoint(bytes);
    }

    public static Point operator +(Point left, Point right)
    {
        var ptr = NativePoint.Add(left.ptr, right.ptr);
        return new Point(ptr);
    }

    public static Point operator -(Point left, Point right)
    {
        var ptr = NativePoint.Sub(left.ptr, right.ptr);
        return new Point(ptr);
    }


    public static Point operator -(Point self)
    {
        var ptr = NativePoint.Negate(self.ptr);
        return new Point(ptr);
    }

    public static Point operator *(Point left, BigInteger right)
    {
        var ptr = NativePoint.Mul(left.ptr, Util.FromBigInteger(right));
        return new Point(ptr);

    }

    public static Point operator *(BigInteger left, Point right)
    {
        var ptr = NativePoint.Mul(right.ptr, Util.FromBigInteger(left));
        return new Point(ptr);
    }

    public static Point operator *(Point left, Scalar right)
    {
        return new Point(NativePoint.Mul(left.ptr, right.ptr));
    }

    public static Point operator *(Scalar left, Point right)
    {
        return new Point(NativePoint.Mul(right.ptr, left.ptr));
    }

    public override bool Equals(object? obj)
    {
        if (obj is Point) {
            return this == (Point) obj;
        } else {
            return false;
        }
    }

    public static bool operator ==(Point left, Point right)
    {
        if (left.ptr == right.ptr) {
            return true;
        }
        return NativePoint.Equals(left.ptr, right.ptr);
    }

    public static bool operator !=(Point left, Point right)
    {
        return !NativePoint.Equals(left.ptr, right.ptr);
    }


    public void GutSpill()
    {
        NativePoint.GutSpill(ptr);
    }

    public override int GetHashCode() => base.GetHashCode();
}

public readonly struct CompressedPoint {

    readonly internal byte[] bytes;

    public CompressedPoint(byte[] bytes)
    {
        if (bytes.Length != 32) {
            throw new ArgumentException("Byte array must be 32 long");
        }
        this.bytes = bytes;
    }


    [DllImport("rust_ffi", EntryPoint = "compressed_ristretto_from_bytes")]
    internal static extern IntPtr FromBytes(byte[] bytes);

    [DllImport("rust_ffi", EntryPoint = "compressed_ristretto_to_bytes")]
    internal static extern void ToBytes(IntPtr self, byte[] bytes);

    public Point Decompress()
    {
        var ptr = NativePoint.Decompress(this.bytes);
        if (ptr == IntPtr.Zero) { // null pointer == could not decompress
            throw new ArgumentException("Could not decompress RistrettoPoint");
        }
        return new Point(ptr);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is not CompressedPoint) {
            return false;
        }
        CompressedPoint other = (CompressedPoint) obj;
        return bytes.SequenceEqual(other.bytes);
    }



    public override int GetHashCode() => base.GetHashCode();

}