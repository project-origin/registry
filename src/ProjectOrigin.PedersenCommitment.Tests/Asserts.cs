namespace ProjectOrigin.PedersenCommitment.Tests;

public static class AssertExt
{
    internal static void SequenceEqual(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual)
    {
        Assert.True(expected.SequenceEqual(actual));
    }
}
