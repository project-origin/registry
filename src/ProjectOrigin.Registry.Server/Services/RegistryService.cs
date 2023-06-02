using Grpc.Core;
using MassTransit;
using ProjectOrigin.Registry.V1;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;
using ProjectOrigin.Registry.Server.Extensions;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace ProjectOrigin.Registry.Server;

public class RegistryService : V1.RegistryService.RegistryServiceBase
{
    private IEventStore _eventStore;
    private IBus _bus;
    private ITransactionStatusService _transactionStatusService;

    public RegistryService(IEventStore eventStore, IBus bus, ITransactionStatusService transactionStatusService)
    {
        _eventStore = eventStore;
        _bus = bus;
        _transactionStatusService = transactionStatusService;
    }

    public override async Task<SubmitTransactionResponse> SendTransactions(SendTransactionsRequest request, ServerCallContext context)
    {
        var jobs = request.Transactions.Select(transaction => new TransactionJob(transaction));

        await _bus.PublishBatch(jobs);

        request.Transactions.AsParallel().ForAll(async transaction =>
        {
            await _transactionStatusService.SetTransactionStatus(
                transaction.GetTransactionId(),
                new V1.Internal.TransactionStatus
                {
                    State = TransactionState.Pending,
                    Message = string.Empty
                });
        });

        return new SubmitTransactionResponse();
    }

    public override async Task<GetTransactionStatusResponse> GetTransactionStatus(GetTransactionStatusRequest request, ServerCallContext context)
    {
        var state = await _transactionStatusService.GetTransactionStatus(request.Id);
        return new GetTransactionStatusResponse
        {
            Status = state.State,
            Message = state.Message,
        };
    }

    public async override Task<GetStreamTransactionsResponse> GetStreamTransactions(V1.GetStreamTransactionsRequest request, ServerCallContext context)
    {
        var streamId = Guid.Parse(request.StreamId.Value);
        var verifiableEvents = await _eventStore.GetEventsForEventStream(streamId);
        var transactions = verifiableEvents.Select(x => V1.Transaction.Parser.ParseFrom(x.Content));

        var response = new GetStreamTransactionsResponse();
        response.Transactions.AddRange(transactions);

        return response;
    }
}
