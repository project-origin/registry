using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Google.Protobuf;
using ProjectOrigin.Registry.V1;

namespace ProjectOrigin.Electricity.Example.Extensions;

public static class RegistryServiceClientExtensions
{
    public static async Task SendTransactionAndWait(this RegistryService.RegistryServiceClient client, Transaction singedTransaction)
    {
        var consClaimRequest = new SendTransactionsRequest();
        consClaimRequest.Transactions.Add(singedTransaction);
        await client.SendTransactionsAsync(consClaimRequest);
        Console.WriteLine("- transaction queued");

        // Wait for status of the transaction to be committed.
        await WaitForCommittedOrTimeout(client, singedTransaction, TimeSpan.FromMinutes(1));
        Console.WriteLine("- transaction committed");
    }

    private static async Task<GetTransactionStatusResponse> WaitForCommittedOrTimeout(
        RegistryService.RegistryServiceClient client,
        Transaction signedTransaction,
        TimeSpan timeout)
    {
        var began = DateTimeOffset.UtcNow;
        var getTransactionStatus = async () => await client.GetTransactionStatusAsync(CreateStatusRequest(signedTransaction));

        while (true)
        {
            var result = await getTransactionStatus();

            if (result.Status == TransactionState.Committed)
                return result;
            else if (result.Status == TransactionState.Failed)
                throw new Exception($"Transaction failed ”{result.Status}” with message ”{result.Message}”");

            await Task.Delay(1000);

            if (began + timeout < DateTimeOffset.UtcNow)
            {
                throw new TimeoutException($"Transaction timed out ”{result.Status}” with message ”{result.Message}”");
            }
        }
    }

    private static GetTransactionStatusRequest CreateStatusRequest(Transaction signedTransaction)
    {
        return new GetTransactionStatusRequest()
        {
            Id = Convert.ToBase64String(SHA256.HashData(signedTransaction.ToByteArray()))
        };
    }

}
