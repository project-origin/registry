using Google.Protobuf;
using NSec.Cryptography;

namespace ProjectOrigin.Electricity.Client;

internal static class Extensions
{
    internal static V1.PublicKey ToProto(this PublicKey key)
    {
        return new V1.PublicKey()
        {
            Content = ByteString.CopyFrom(key.Export(KeyBlobFormat.RawPublicKey))
        };
    }
}
