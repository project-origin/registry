using Google.Protobuf;
using ProjectOrigin.Electricity.Interfaces;

namespace ProjectOrigin.Electricity.Extensions;

public static class ProtoExtensions
{
    public static bool IsSignatureValid(this Registry.V1.Transaction transaction, IPublicKey publicKey)
    {
        return publicKey.VerifySignature(transaction.Header.ToByteArray(), transaction.HeaderSignature.Value.Span);
    }
}
