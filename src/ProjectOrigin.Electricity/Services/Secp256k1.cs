using NBitcoin;
using ProjectOrigin.Electricity.Interfaces;
using ProjectOrigin.Registry.V1;

namespace ProjectOrigin.WalletSystem.Server.HDWallet;

public class Secp256k1Algorithm : IKeyAlgorithm
{
    public IPublicKey ImportPublicKey(ReadOnlySpan<byte> span)
    {
        return new Secp256k1PublicKey(span);
    }

    public bool TryImport(ReadOnlySpan<byte> span, out IPublicKey keyOut)
    {
        try
        {
            keyOut = new Secp256k1PublicKey(span);
            return true;
        }
        catch (Exception)
        {
            keyOut = null!;
            return false;
        }
    }

    private static uint256 HashData(ReadOnlySpan<byte> data)
    {
        return new uint256(NBitcoin.Crypto.Hashes.SHA256(data));
    }

    internal class Secp256k1PublicKey : IPublicKey
    {
        private PubKey _pubKey;

        public Secp256k1PublicKey(ReadOnlySpan<byte> span)
        {
            _pubKey = new PubKey(span);
        }

        public bool VerifySignature(ReadOnlySpan<byte> data, Signature signature)
        {
            return _pubKey.Verify(HashData(data), new NBitcoin.Crypto.ECDSASignature(signature.Value.Span));
        }
    }
}
