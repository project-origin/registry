using System;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore;

public static class BlockSizeCalculator
{
    private const int MaxExponent = 20;

    /// <summary>
    /// Calculates the block length based on the available number of transactions.
    /// </summary>
    public static long CalculateBlockLength(long availableNumberOfTransactions)
    {
        var exponent = Math.Log(availableNumberOfTransactions, 2);
        exponent = Math.Ceiling(exponent);

        exponent = Math.Min(exponent, MaxExponent);
        var maxBasedOnExponent = 1L << (int)exponent;

        var blockLength = Math.Min(maxBasedOnExponent, availableNumberOfTransactions);

        return blockLength;
    }
}
