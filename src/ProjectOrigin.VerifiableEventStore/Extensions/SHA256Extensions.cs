
using System.Linq;
using System.Security.Cryptography;

namespace ProjectOrigin.VerifiableEventStore.Extensions;

public class SHA256Array
{
    public static byte[] HashData(params byte[][] data)
    {
        return SHA256.HashData(data.SelectMany(x => x).ToArray());
    }
}
