namespace System.Numerics;

public static class RandomExtensions
{
    const int bitsInByte = 8;

    /// <summary>
    /// Generates a random BigInteger between 0 and 2^bits - 1.
    /// </summary>
    /// <param name="bits">The number of random bits to generate.</param>
    public static BigInteger NextBigInteger(this Random self, int bits)
    {
        if (bits < 1) return BigInteger.Zero;

        int neededBytes = bits / bitsInByte + 1;
        int maskShift = bitsInByte - bits % bitsInByte;

        byte[] bytes = new byte[neededBytes];
        self.NextBytes(bytes);

        bytes[bytes.Length - 1] &= (byte)(0xFF >> maskShift);

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

        BigInteger range = end - start;

        byte[] bytes = range.ToByteArray();

        int bits = 0;
        while ((2 ^ bits) < (int)bytes[bytes.Length - 1])
        {
            bits++;
        }
        bits += bitsInByte * bytes.Length - 1;

        return ((self.NextBigInteger(bits) * range) / BigInteger.Pow(2, bits)) + start;
    }
}
