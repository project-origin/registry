namespace System.Numerics;

public static class BigIntegerExtensions
{
    const int defaultWitnesses = 10;

    public static bool IsProbablyNotPrime(this BigInteger number, int witnesses = defaultWitnesses)
    {
        return !number.IsProbablyPrime(witnesses);
    }



    /// <summary>
    /// Checks that a BigInteger is probably a prime based
    /// https://stackoverflow.com/a/33918233
    /// </summary>
    public static Boolean IsProbablyPrime(this BigInteger number, int witnesses = defaultWitnesses)
    {
        if (number <= 1)
            return false;

        if (witnesses <= 0)
            witnesses = 10;

        BigInteger d = number - 1;
        int s = 0;

        while (d % 2 == 0)
        {
            d /= 2;
            s += 1;
        }

        Byte[] bytes = new Byte[number.GetByteCount()];
        BigInteger a;

        for (int i = 0; i < witnesses; i++)
        {
            do
            {
                Random.Shared.NextBytes(bytes);

                a = new BigInteger(bytes);
            }
            while (a < 2 || a >= number - 2);

            BigInteger x = BigInteger.ModPow(a, d, number);
            if (x == 1 || x == number - 1)
                continue;

            for (int r = 1; r < s; r++)
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
