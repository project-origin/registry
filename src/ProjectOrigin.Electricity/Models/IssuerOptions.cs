using NSec.Cryptography;

namespace ProjectOrigin.Electricity.Models;

public class IssuerOptions
{
    public Dictionary<string, string> Issuers { get; set; } = new Dictionary<string, string>();

    public bool IsValid => Issuers.All(set =>
    {
        return
            PublicKey.TryImport(
                SignatureAlgorithm.Ed25519,
                Convert.FromBase64String(set.Value),
                KeyBlobFormat.RawPublicKey,
                out _);
    }) && Issuers.Any();

    public PublicKey? GetAreaPublicKey(string area)
    {
        if (Issuers.TryGetValue(area, out var base64data))
        {
            return PublicKey.Import(SignatureAlgorithm.Ed25519, Convert.FromBase64String(base64data), KeyBlobFormat.RawPublicKey);
        }
        return null;
    }
}
