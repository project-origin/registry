using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using SimpleBase;

public class DerivePublicKey
{
    private string base58PrivateKey;

    public DerivePublicKey(string base58PrivateKey)
    {
        this.base58PrivateKey = base58PrivateKey;
    }

    public Task<int> Run()
    {
        var algorithm = new Secp256k1Algorithm();

        var bytes = Base58.Bitcoin.Decode(base58PrivateKey);
        var privateKey = algorithm.ImportHDPrivateKey(bytes);
        Console.WriteLine(Base58.Bitcoin.Encode(privateKey.PublicKey.Export()));

        return Task.FromResult(0);
    }
}
