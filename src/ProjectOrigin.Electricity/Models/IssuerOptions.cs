using ProjectOrigin.Electricity.Interfaces;

namespace ProjectOrigin.Electricity.Models;

public class IssuerOptions
{
    private IKeyAlgorithm _keyAlgorithm;

    public Dictionary<string, string> Issuers { get; set; } = new Dictionary<string, string>();

    public IssuerOptions(IKeyAlgorithm keyAlgorithm)
    {
        _keyAlgorithm = keyAlgorithm;
    }

    public bool IsValid => Issuers.All(set =>
    {
        throw new NotImplementedException();
        // return
        //     PublicKey.TryImport(
        //         SignatureAlgorithm.Ed25519,
        //         Convert.FromBase64String(set.Value),
        //         KeyBlobFormat.RawPublicKey,
        //         out _);
    }) && Issuers.Any();

    public IPublicKey? GetAreaPublicKey(string area)
    {
        if (Issuers.TryGetValue(area, out var base64data))
        {
            return _keyAlgorithm.ImportPublicKey(Convert.FromBase64String(base64data));
        }
        return null;
    }
}
