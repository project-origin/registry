using Grpc.Core;
using MassTransit;
using ProjectOrigin.Registry.V1;
using System.Threading.Tasks;
using System.Linq;
using System;
using ProjectOrigin.VerifiableEventStore.Services.TransactionStatusCache;
using System.Security.Cryptography;
using Google.Protobuf;
using System.Diagnostics.Metrics;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.Repository;

namespace ProjectOrigin.Registry.Server;

public class RegistryService : V1.RegistryService.RegistryServiceBase
{
    public static Meter Meter = new("Registry.RegistryService");
    public static Counter<long> TransactionsSubmitted = Meter.CreateCounter<long>("TransactionsSubmitted");

    private ITransactionRepository _eventStore;
    private IBus _bus;
    private ITransactionStatusService _transactionStatusService;

    public RegistryService(ITransactionRepository eventStore, IBus bus, ITransactionStatusService transactionStatusService)
    {
        _eventStore = eventStore;
        _bus = bus;
        _transactionStatusService = transactionStatusService;
    }

    public override async Task<SubmitTransactionResponse> SendTransactions(SendTransactionsRequest request, ServerCallContext context)
    {
        foreach (var transaction in request.Transactions)
        {
            var message = VerifyTransaction.Create(transaction);
            var transactionHash = new TransactionHash(SHA256.HashData(transaction.ToByteArray()));

            await _transactionStatusService.SetTransactionStatus(
                transactionHash,
                new TransactionStatusRecord(TransactionStatus.Pending)
                )
                .ConfigureAwait(false);

            await _bus.Publish(message).ConfigureAwait(false); // Should be reworked to Send() to a specific exchange once it is implemented
        }

        TransactionsSubmitted.Add(request.Transactions.Count);

        return new SubmitTransactionResponse();
    }

    public override async Task<GetTransactionStatusResponse> GetTransactionStatus(GetTransactionStatusRequest request, ServerCallContext context)
    {
        var transactionHash = new TransactionHash(Convert.FromBase64String(request.Id));
        var state = await _transactionStatusService.GetTransactionStatus(transactionHash).ConfigureAwait(false);
        return new GetTransactionStatusResponse
        {
            Status = (V1.TransactionState)state.NewStatus,
            Message = state.Message,
        };
    }

    public async override Task<GetStreamTransactionsResponse> GetStreamTransactions(V1.GetStreamTransactionsRequest request, ServerCallContext context)
    {
        var streamId = Guid.Parse(request.StreamId.Value);
        var verifiableEvents = await _eventStore.GetStreamTransactionsForStream(streamId).ConfigureAwait(false);
        var transactions = verifiableEvents.Select(x => V1.Transaction.Parser.ParseFrom(x.Payload));

        var response = new GetStreamTransactionsResponse();
        response.Transactions.AddRange(transactions);

        return response;
    }
}
