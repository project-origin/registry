using System;
using Microsoft.Extensions.Options;
using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore;

public class BlockSizeCalculator
{
    private readonly int _maxExponent;

    public BlockSizeCalculator(IOptions<VerifiableEventStoreOptions> options)
    {
        _maxExponent = options.Value.MaxExponent;
    }

    /// <summary>
    /// Calculates the block length based on the available number of transactions.
    /// </summary>
    public long CalculateBlockLength(long availableNumberOfTransactions)
    {
        var exponent = Math.Log(availableNumberOfTransactions, 2);
        exponent = Math.Ceiling(exponent);

        exponent = Math.Min(exponent, _maxExponent);
        var maxBasedOnExponent = 1L << (int)exponent;

        var blockLength = Math.Min(maxBasedOnExponent, availableNumberOfTransactions);

        return blockLength;
    }
}
