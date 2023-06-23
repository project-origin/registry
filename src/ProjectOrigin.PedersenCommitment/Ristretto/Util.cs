using System;
using System.Runtime.InteropServices;
namespace ProjectOrigin.PedersenCommitment.Ristretto;

public static class Extensions
{
    internal static byte[] ToByteArray(this ulong value, uint arrayLength)
    {
        var inputBytes = BitConverter.GetBytes(value);
        var length = inputBytes.Length;

        if (arrayLength < length)
        {
            throw new ArgumentException("Wanted Length is smaller that source.");
        }

        var outputArray = new byte[arrayLength];
        Array.Copy(inputBytes, outputArray, inputBytes.Length);
        return outputArray;
    }

    [DllImport("rust_ffi", EntryPoint = "fill_bytes")]
    internal static extern void FillBytes(RawVec raw, byte[] dst);

    [DllImport("rust_ffi", EntryPoint = "free_vec")]
    internal static extern void FreeVec(RawVec raw);

    [StructLayout(LayoutKind.Sequential)]
    internal struct RawVec
    {
        internal IntPtr data;
        internal nuint size;
        internal nuint cap;
    }
}
