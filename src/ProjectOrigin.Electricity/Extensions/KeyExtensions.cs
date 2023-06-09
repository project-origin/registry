using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;

namespace ProjectOrigin.Electricity.Extensions;

public static class KeyExtensions
{
    public static bool TryImport(this IHDAlgorithm algorithm, ReadOnlySpan<byte> publicKeyBytes, out IPublicKey key)
    {
        try
        {
            key = algorithm.ImportPublicKey(publicKeyBytes);
            return true;
        }
        catch
        {
            key = default!;
            return false;
        }
    }
}
