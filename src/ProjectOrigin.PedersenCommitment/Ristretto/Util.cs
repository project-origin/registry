namespace ProjectOrigin.PedersenCommitment.Ristretto;

internal static class Extensions
{
    internal static byte[] ToByteArray(this ulong value, uint arrayLength)
    {
        var inputBytes = BitConverter.GetBytes(value);
        var length = inputBytes.Length;

        if (arrayLength < length)
        {
            throw new ArgumentException("WantedLength is smaller that source.");
        }

        var outputArray = new byte[arrayLength];
        Array.Copy(inputBytes, outputArray, inputBytes.Length);
        return outputArray;
    }
}
