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

    public IPrivateKey Create()
    {
        return new Secp256k1PrivateKey(new Key());
    }

    internal class Secp256k1PublicKey : IPublicKey
    {
        private PubKey _pubKey;

        public Secp256k1PublicKey(ReadOnlySpan<byte> span)
        {
            _pubKey = new PubKey(span);
        }

        public Secp256k1PublicKey(PubKey key)
        {
            _pubKey = key;
        }

        public ReadOnlySpan<byte> Export()
        {
            return _pubKey.ToBytes();
        }

        public bool VerifySignature(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature)
        {
            var result = _pubKey.Verify(HashData(data), new NBitcoin.Crypto.ECDSASignature(signature));
            return result;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Secp256k1PublicKey)
            {
                return _pubKey.Equals(((Secp256k1PublicKey)obj)._pubKey);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return _pubKey.GetHashCode();
        }
    }

    internal class Secp256k1PrivateKey : IPrivateKey
    {
        private Key _key;

        public Secp256k1PrivateKey(Key key)
        {
            _key = key;
        }

        public IPublicKey PublicKey => new Secp256k1PublicKey(_key.PubKey);

        public ReadOnlySpan<byte> Sign(ReadOnlySpan<byte> data)
        {
            return _key.Sign(HashData(data)).ToDER();
        }
    }
}
