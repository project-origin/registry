using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;

public class GenerateKey
{
    private string _filename;

    public GenerateKey(string filename)
    {
        _filename = filename;
    }

    public async Task<int> Run()
    {
        var algorithm = new Secp256k1Algorithm();
        var privateKey = algorithm.GenerateNewPrivateKey();

        await WriteToFile(Convert.ToBase64String(privateKey.Export()), $"{_filename}.key");
        await WriteToFile(Convert.ToBase64String(privateKey.PublicKey.Export()), $"{_filename}.pub");

        return 0;
    }

    private async Task WriteToFile(string data, string filepath)
    {
        using (StreamWriter outputFile = new StreamWriter(Path.Combine(Environment.CurrentDirectory, filepath)))
        {
            await outputFile.WriteAsync(data);
        }
    }
}
