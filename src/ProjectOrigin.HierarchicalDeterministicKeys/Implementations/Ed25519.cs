using System;
using System.Text;
using NSec.Cryptography;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;

public class Ed25519Algorithm
{
    private static readonly SignatureAlgorithm algorithm = SignatureAlgorithm.Ed25519;

    /// <summary>
    /// Import a private key from a byte array
    /// containg the text representation of the key.
    /// </summary>
    public IPrivateKey ImportPrivateKeyText(string keyText)
    {
        var bytes = Encoding.UTF8.GetBytes(keyText);
        var key = Key.Import(algorithm, bytes, KeyBlobFormat.PkixPrivateKeyText);
        return new Ed25519PrivateKey(key);
    }

    /// <summary>
    /// Import a public key from a byte array
    /// In the PKIX text format.
    /// </summary>
    public IPublicKey ImportPublicKeyText(string keyText)
    {
        var bytes = Encoding.UTF8.GetBytes(keyText);
        var key = PublicKey.Import(algorithm, bytes, KeyBlobFormat.PkixPublicKeyText);
        return new Ed25519PublicKey(key);
    }

    /// <summary>
    /// Import a private key from a byte array containg the key.
    /// In the PKIX binary format.
    /// </summary>
    public IPublicKey ImportPublicKey(ReadOnlySpan<byte> span)
    {
        var key = PublicKey.Import(algorithm, span, KeyBlobFormat.PkixPublicKey);
        return new Ed25519PublicKey(key);
    }

    public IPrivateKey GenerateNewPrivateKey()
    {
        var key = new Key(algorithm, new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });
        return new Ed25519PrivateKey(key);
    }

    public class Ed25519PrivateKey : IPrivateKey
    {
        private Key _key;

        public Ed25519PrivateKey(Key key)
        {
            _key = key;
        }

        public IPublicKey PublicKey => new Ed25519PublicKey(_key.PublicKey);

        public ReadOnlySpan<byte> Export()
        {
            return _key.Export(KeyBlobFormat.PkixPrivateKey);
        }

        public string ExportPkixText()
        {
            return Encoding.UTF8.GetString(_key.Export(KeyBlobFormat.PkixPrivateKeyText));
        }

        public ReadOnlySpan<byte> Sign(ReadOnlySpan<byte> data)
        {
            return algorithm.Sign(_key, data);
        }
    }

    public class Ed25519PublicKey : IPublicKey
    {
        private PublicKey _key;

        public Ed25519PublicKey(PublicKey key)
        {
            _key = key;
        }

        public ReadOnlySpan<byte> Export()
        {
            return _key.Export(KeyBlobFormat.PkixPublicKey);
        }

        public string ExportPkixText()
        {
            return Encoding.UTF8.GetString(_key.Export(KeyBlobFormat.PkixPublicKeyText));
        }

        public bool Verify(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature)
        {
            return algorithm.Verify(_key, data, signature);
        }
    }
}
