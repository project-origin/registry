using System;
using Google.Protobuf;
using ProjectOrigin.HierarchicalDeterministicKeys;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;

namespace ProjectOrigin.Electricity.Extensions;

public static class SignatureExtensions
{
    public static IPublicKey ToModel(this Electricity.V1.PublicKey protoKey)
    {
        switch (protoKey.Type)
        {
            case Electricity.V1.KeyType.Secp256K1:
                {
                    return Algorithms.Secp256k1.ImportPublicKey(protoKey.Content.Span);
                }
            case Electricity.V1.KeyType.Ed25519:
                {
                    return Algorithms.Ed25519.ImportPublicKey(protoKey.Content.Span);
                }
            default:
                throw new NotSupportedException();
        }
    }

    public static bool TryToModel(this Electricity.V1.PublicKey protoKey, out IPublicKey publicKey)
    {
        try
        {
            publicKey = protoKey.ToModel();
            return true;
        }
        catch
        {
            publicKey = default!;
            return false;
        }
    }

    public static bool IsSignatureValid(this Registry.V1.Transaction transaction, Electricity.V1.PublicKey publicKey)
    {
        return publicKey.ToModel().Verify(transaction.Header.ToByteArray(), transaction.HeaderSignature.Span);
    }

    public static bool IsSignatureValid(this Registry.V1.Transaction transaction, IPublicKey publicKey)
    {
        return publicKey.Verify(transaction.Header.ToByteArray(), transaction.HeaderSignature.Span);
    }
}
