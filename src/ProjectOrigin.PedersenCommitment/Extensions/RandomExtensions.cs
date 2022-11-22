namespace System.Numerics;

public static class RandomExtensions
{
    const int BitsInByte = 8;

    /// <summary>
    /// Generates a random BigInteger between 0 and pow(2,bits) - 1.
    /// </summary>
    /// <param name="bits">The number of random bits to generate.</param>
    public static BigInteger NextBigInteger(this Random self, int bits)
    {
        if (bits < 1) return BigInteger.Zero;

        var max = BigInteger.Pow(2, bits);

        var bytes = new byte[max.GetByteCount()];
        self.NextBytes(bytes);

        var lastIndex = bytes.Length - 1;
        bytes[lastIndex] &= max.ToByteArray()[lastIndex];

        return new BigInteger(bytes);
    }

    /// <summary>
    /// Generates a random BigInteger between two numbers.
    /// </summary>
    /// <param name="start">The lower bound.</param>
    /// <param name="end">The upper bound (non-inclusive).</param>
    /// <returns>A random BigInteger between start and end (non-inclusive)</returns>
    public static BigInteger NextBigInteger(this Random self, BigInteger start, BigInteger end)
    {
        if (start > end) throw new InvalidDataException("Start must be smaller or equal to end.");
        if (start == end) return start;

        var range = end - start;
        var bits = (int)range.GetBitLength();

        return ((self.NextBigInteger(bits) * range) / BigInteger.Pow(2, bits)) + start;
    }
}
