using System.Runtime.InteropServices;
namespace ProjectOrigin.PedersenCommitment.Ristretto;

internal class Native {
    // TODO: this might not work
    [DllImport("rust_ffi", EntryPoint = "ristretto_point_compress")]
    internal static extern void Compress(IntPtr self, byte[] bytes_ptr);

    // TODO: this might not work
    [DllImport("rust_ffi", EntryPoint = "ristretto_point_decompress")]
    internal static extern IntPtr Decompress(CompressedPoint point);

    [DllImport("rust_ffi", EntryPoint = "ristretto_point_free")]
    internal static extern void Dispose(IntPtr self);

    [DllImport("rust_ffi", EntryPoint = "ristretto_point_add")]
    internal static extern IntPtr Add(IntPtr lhs, IntPtr rhs);
}

public record Point : IDisposable
{

    private IntPtr ptr;

    internal Point(IntPtr ptr)
    {
        this.ptr = ptr;
    }

    public void Dispose()
    {
        Native.Dispose(ptr);
    }

    public CompressedPoint Compress() {
        var bytes = new byte[32];
        var str = Convert.ToBase64String(bytes);
        Console.WriteLine(str);
        Native.Compress(ptr, bytes);
        return new CompressedPoint(bytes);

    }

    public static Point operator +(Point left, Point right)
    {
        var ptr = Native.Add(left.ptr, right.ptr);
        return new Point(ptr);
    }

}

public readonly struct CompressedPoint {

    readonly public byte[] bytes;

    public CompressedPoint(byte[] bytes)
    {
        if (bytes.Length != 32) {
            throw new ArgumentException("Byte array must be 32 long");
        }
        this.bytes = bytes;
    }


    public Point Decompress() {
        // TODO: Error handling, as decompress can fail
        return new Point(Native.Decompress(this));
    }


}
