public class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelpInfo();
            return 0;
        }

        string command = args[0];
        switch (command)
        {
            case "GenerateKey":
                if (args.Length < 2)
                {
                    Console.Error.WriteLine("Insufficient arguments for 'GenerateKey'");
                    return 1;
                }
                string name = args[1];
                var flow = new GenerateKey(name);
                return await flow.Run();

            case "WithoutWalletFlow":
                if (args.Length < 7)
                {
                    Console.Error.WriteLine("Insufficient arguments for 'WithoutWalletFlow'");
                    return 1;
                }
                string area = args[1];
                string signerKeyPath = args[2];
                string prodRegistryName = args[3];
                string prodRegistryAddress = args[4];
                string consRegistryName = args[5];
                string consRegistryAddress = args[6];
                var flow2 = new WithoutWalletFlow(area, signerKeyPath, prodRegistryName, prodRegistryAddress, consRegistryName, consRegistryAddress);
                return await flow2.Run();

            default:
                Console.Error.WriteLine("Invalid command");
                PrintHelpInfo();
                return 1;
        }
    }

    static void PrintHelpInfo()
    {
        Console.WriteLine("Usage: programName [command] [args]");
        Console.WriteLine("Available commands:");
        Console.WriteLine("  GenerateKey [Filename]");
        Console.WriteLine("  WithoutWalletFlow [Area] [SignerKeyPath] [ProdRegistryName] [ProdRegistryAddress] [ConsRegistryName] [ConsRegistryAddress]");
    }
}
