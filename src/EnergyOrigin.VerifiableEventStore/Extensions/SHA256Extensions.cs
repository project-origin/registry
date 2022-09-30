
using System.Security.Cryptography;

namespace EnergyOrigin.VerifiableEventStore.Extensions;

public class SHA256Array
{
    public static byte[] HashData(params byte[][] data)
    {
        return SHA256.HashData(data.SelectMany(x => x).ToArray());
    }
}
