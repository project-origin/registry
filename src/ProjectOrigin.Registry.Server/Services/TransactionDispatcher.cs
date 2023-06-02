using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using ProjectOrigin.Registry.Server.Interfaces;
using ProjectOrigin.Registry.Server.Models;
using ProjectOrigin.Registry.V1;
using ProjectOrigin.Verifier.V1;

namespace ProjectOrigin.Registry.Server.Services;

public class TransactionDispatcher : ITransactionDispatcher
{
    private readonly ConcurrentDictionary<string, Lazy<VerifierService.VerifierServiceClient>> concurrentDictionary;
    private readonly VerifierOptions _options;

    public TransactionDispatcher(IOptions<VerifierOptions> options)
    {
        concurrentDictionary = new ConcurrentDictionary<string, Lazy<VerifierService.VerifierServiceClient>>();
        _options = options.Value;
    }

    public async Task<VerifyTransactionResponse> VerifyTransaction(Transaction transaction, IEnumerable<Transaction> stream)
    {
        var index = transaction.Header.PayloadType.LastIndexOf(".");
        var family = transaction.Header.PayloadType.Substring(0, index);

        var client = GetClient(family);
        var request = new VerifyTransactionRequest()
        {
            Transaction = transaction
        };
        request.Stream.AddRange(stream);

        return await client.VerifyTransactionAsync(request);
    }

    public VerifierService.VerifierServiceClient GetClient(string family)
    {
        var lazy = concurrentDictionary.GetOrAdd(family, family => new Lazy<VerifierService.VerifierServiceClient>(() => CreateClient(family)));
        return lazy.Value;
    }

    private VerifierService.VerifierServiceClient CreateClient(string family)
    {
        if (_options.Verifiers.TryGetValue(family, out var url))
        {
            var channel = GrpcChannel.ForAddress(url);
            return new VerifierService.VerifierServiceClient(channel);
        }
        else
        {
            throw new Exception($"No verifier found for transaction family {family}");
        }
    }
}
