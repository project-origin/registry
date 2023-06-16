﻿public class Program
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
                if (args.Length < 5)
                {
                    Console.Error.WriteLine("Insufficient arguments for 'WithoutWalletFlow'");
                    return 1;
                }
                string registryName = args[1];
                string registryAddress = args[2];
                string area = args[3];
                string signerKeyPath = args[4];
                var flow2 = new WithoutWalletFlow(registryName, registryAddress, area, signerKeyPath);
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
        Console.WriteLine("  WithoutWalletFlow [RegistryName] [RegistryAddress] [Area] [SignerKeyPath]");
    }
}