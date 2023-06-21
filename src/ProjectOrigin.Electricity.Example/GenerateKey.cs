using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using SimpleBase;

public class GeneratePrivateKey
{
    public Task<int> Run()
    {
        var algorithm = new Secp256k1Algorithm();

        var privateKey = algorithm.GenerateNewPrivateKey();

        Console.WriteLine(Base58.Bitcoin.Encode(privateKey.Export()));

        return Task.FromResult(0);
    }
}
