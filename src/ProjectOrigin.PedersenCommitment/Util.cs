namespace ProjectOrigin.PedersenCommitment;
using System.Numerics;

internal class Util
{
    internal static byte[] FromBigInteger(BigInteger b)
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
