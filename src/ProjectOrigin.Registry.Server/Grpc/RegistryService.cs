using Grpc.Core;
using ProjectOrigin.Registry.V1;
using System.Threading.Tasks;
using System.Linq;
using System;
using ProjectOrigin.VerifiableEventStore.Services.TransactionStatusCache;
using System.Diagnostics.Metrics;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.Repository;
using ProjectOrigin.Registry.Server.Extensions;
using Google.Protobuf;
using ProjectOrigin.Registry.Server.Interfaces;
using RabbitMQ.Client;

namespace ProjectOrigin.Registry.Server.Grpc;

public class RegistryService : V1.RegistryService.RegistryServiceBase
{
    public static readonly Meter Meter = new("Registry.RegistryService");
    public static readonly Counter<long> TransactionsSubmitted = Meter.CreateCounter<long>("TransactionsSubmitted");

    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionStatusService _transactionStatusService;
    private readonly IRabbitMqChannelPool _brokerPool;
    private readonly IQueueResolver _queueResolver;

    public RegistryService(
        ITransactionRepository transactionRepository,
        ITransactionStatusService transactionStatusService,
        IRabbitMqChannelPool brokerPool,
        IQueueResolver queueResolver)
    {
        _transactionRepository = transactionRepository;
        _transactionStatusService = transactionStatusService;
        _brokerPool = brokerPool;
        _queueResolver = queueResolver;
    }

    public override async Task<SubmitTransactionResponse> SendTransactions(SendTransactionsRequest request, ServerCallContext context)
    {
        using (var brokerChannel = _brokerPool.GetChannel())
        {
            foreach (var transaction in request.Transactions)
            {
                await _transactionStatusService.SetTransactionStatus(
                        transaction.GetTransactionHash(),
                        new TransactionStatusRecord(TransactionStatus.Pending)
                    )
                    .ConfigureAwait(false);

                var queue = _queueResolver.GetQueueName(transaction);

                await brokerChannel.Channel.BasicPublishAsync("", queue, transaction.ToByteArray())
                    .ConfigureAwait(false);
            }
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
        var verifiableEvents = await _transactionRepository.GetStreamTransactionsForStream(streamId).ConfigureAwait(false);
        var transactions = verifiableEvents.Select(x => V1.Transaction.Parser.ParseFrom(x.Payload));

        var response = new GetStreamTransactionsResponse();
        response.Transactions.AddRange(transactions);

        return response;
    }
}
