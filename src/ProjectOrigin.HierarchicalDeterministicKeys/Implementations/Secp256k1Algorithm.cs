using System;
using NBitcoin;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;

namespace ProjectOrigin.HierarchicalDeterministicKeys.Implementations;

public class Secp256k1Algorithm : IHDAlgorithm
{
    public IHDPrivateKey GenerateNewPrivateKey()
    {
        return new Secp256k1HDPrivateKey(new ExtKey());
    }

    public IHDPrivateKey ImportHDPrivateKey(ReadOnlySpan<byte> privateKeyBytes)
    {
        return new Secp256k1HDPrivateKey(ExtKey.CreateFromBytes(privateKeyBytes));

    }

    public IHDPublicKey ImportHDPublicKey(ReadOnlySpan<byte> publicKeyBytes)
    {
        return new Secp256k1HDPublicKey(new ExtPubKey(publicKeyBytes));
    }

    public IPublicKey ImportPublicKey(ReadOnlySpan<byte> privateKeyBytes)
    {
        return new Secp256k1PublicKey(new PubKey(privateKeyBytes));
    }

    private static uint256 HashData(ReadOnlySpan<byte> data)
    {
        return new uint256(NBitcoin.Crypto.Hashes.SHA256(data));
    }

    public IPrivateKey New()
    {
        throw new NotImplementedException();
    }

    internal class Secp256k1HDPrivateKey : IHDPrivateKey
    {
        private readonly ExtKey _key;

        public Secp256k1HDPrivateKey(ExtKey key)
        {
            _key = key;

            var a = Network.Main.CreateBitcoinExtKey(key);

            a.ToString();
        }

        public ReadOnlySpan<byte> Sign(ReadOnlySpan<byte> data)
        {
            return _key.PrivateKey.Sign(HashData(data)).ToDER();
        }

        public IPublicKey PublicKey => new Secp256k1PublicKey(_key.GetPublicKey());

        public ReadOnlySpan<byte> Export() => _key.ToBytes();

        public IHDPrivateKey Derive(int position) => new Secp256k1HDPrivateKey(_key.Derive((uint)position));

        public IHDPublicKey Neuter() => new Secp256k1HDPublicKey(_key.Neuter());

        public string ExportPkixText()
        {
            return "-----BEGIN PRIVATE KEY-----\n" +
                Convert.ToBase64String(_key.PrivateKey.ToBytes()) +
                "\n-----END PRIVATE KEY-----";
        }
    }

    internal class Secp256k1HDPublicKey : IHDPublicKey
    {
        private ExtPubKey _extPubKey;

        public Secp256k1HDPublicKey(ExtPubKey extPubKey)
        {
            _extPubKey = extPubKey;
        }

        public IHDPublicKey Derive(int position) => new Secp256k1HDPublicKey(_extPubKey.Derive((uint)position));

        public ReadOnlySpan<byte> Export() => this._extPubKey.ToBytes();

        public IPublicKey GetPublicKey()
        {
            return new Secp256k1PublicKey(_extPubKey.GetPublicKey());
        }

        public bool Verify(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature)
        {
            return GetPublicKey().Verify(data, signature);
        }
    }

    internal class Secp256k1PublicKey : IPublicKey
    {
        private PubKey _pubKey;

        public Secp256k1PublicKey(PubKey pubKey)
        {
            _pubKey = pubKey;
        }

        public ReadOnlySpan<byte> Export()
        {
            return _pubKey.ToBytes();
        }

        public bool Verify(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature)
        {
            return _pubKey.Verify(HashData(data), new NBitcoin.Crypto.ECDSASignature(signature));
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

        public string ExportPkixText()
        {
            return "-----BEGIN PUBLIC KEY-----\n" +
                Convert.ToBase64String(_pubKey.ToBytes()) +
                "\n-----END PUBLIC KEY-----";
        }
    }
}
