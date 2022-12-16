namespace System.Numerics;

internal static class BigIntegerExtensions
{
    const int DefaultWitnesses = 10;

    /// <summary>
    /// Mathematical modulus where negative devidend modulo a positive devisor
    /// returns a positiv result.
    /// example: -5 modulo 3 returns 1
    /// </summary>
    public static BigInteger MathMod(this BigInteger dividend, BigInteger divisor)
    {
        return (BigInteger.Abs(dividend * divisor) + dividend) % divisor;
    }

    public static bool IsProbablyNotPrime(this BigInteger number, int witnesses = DefaultWitnesses)
    {
        return !number.IsProbablyPrime(witnesses);
    }

    /// <summary>
    /// Checks that a BigInteger is probably a prime based
    /// https://stackoverflow.com/a/33918233
    /// </summary>
    public static bool IsProbablyPrime(this BigInteger number, int witnesses = DefaultWitnesses)
    {
        if (number <= 1)
            return false;

        if (witnesses <= 0)
            witnesses = 10;

        var d = number - 1;
        var s = 0;

        while (d % 2 == 0)
        {
            d /= 2;
            s += 1;
        }

        var bytes = new byte[number.GetByteCount()];
        BigInteger a;

        for (var i = 0; i < witnesses; i++)
        {
            do
            {
                Random.Shared.NextBytes(bytes);

                a = new BigInteger(bytes);
            }
            while (a < 2 || a >= number - 2);

            var x = BigInteger.ModPow(a, d, number);
            if (x == 1 || x == number - 1)
                continue;

            for (var r = 1; r < s; r++)
            {
                x = BigInteger.ModPow(x, 2, number);

                if (x == 1)
                    return false;
                if (x == number - 1)
                    break;
            }

            if (x != number - 1)
                return false;
        }

        return true;
    }
}
