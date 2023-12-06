using System;
using ProjectOrigin.Electricity.Example;

if (args.Length == 0)
{
    PrintHelpInfo();
    return 0;
}

string command = args[0];
switch (command)
{
    case "WithoutWalletFlow":
        {

            if (args.Length < 7)
            {
                Console.Error.WriteLine("Insufficient arguments for 'WithoutWalletFlow'");
                return 1;
            }

            var flow = new WithoutWalletFlow
            {
                Area = args[1],
                IssuerKey = args[2],
                ProdRegistryName = args[3],
                ProdRegistryAddress = args[4],
                ConsRegistryName = args[5],
                ConsRegistryAddress = args[6],
            };
            return await flow.Run();
        }

    case "WithWalletFlow":
        {

            if (args.Length < 8)
            {
                Console.Error.WriteLine("Insufficient arguments for 'WithWalletFlow'");
                return 1;
            }
            var flow = new WithWalletFlow
            {
                Area = args[1],
                IssuerKey = args[2],
                ProdRegistryName = args[3],
                ProdRegistryAddress = args[4],
                ConsRegistryName = args[5],
                ConsRegistryAddress = args[6],
                WalletAddress = args[7],
            };
            return await flow.Run();
        }

    default:
        Console.Error.WriteLine("Invalid command");
        PrintHelpInfo();
        return 1;
}

void PrintHelpInfo()
{
    Console.WriteLine("Usage: programName [command] [args]");
    Console.WriteLine("Available commands:");
    Console.WriteLine("  WithoutWalletFlow [Area] [SignerKey] [ProdRegistryName] [ProdRegistryAddress] [ConsRegistryName] [ConsRegistryAddress]");
    Console.WriteLine("  WithWalletFlow [Area] [SignerKey] [ProdRegistryName] [ProdRegistryAddress] [ConsRegistryName] [ConsRegistryAddress] [WalletAddress]");
}
