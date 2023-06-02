using System;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using MassTransit;
using Microsoft.Extensions.Options;
using ProjectOrigin.Registry.Server.Exceptions;
using ProjectOrigin.Registry.Server.Extensions;
using ProjectOrigin.Registry.Server.Interfaces;
using ProjectOrigin.Registry.Server.Models;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.Registry.Server.Services;

public class TransactionProcessor : IJobConsumer<TransactionJob>
{
    private TransactionProcessorOptions _options;
    private IEventStore _eventStore;
    private ITransactionDispatcher _verifier;
    private ITransactionStatusService _transactionStatusService;

    public TransactionProcessor(IOptions<TransactionProcessorOptions> options,
                                IEventStore localEventStore,
                                ITransactionDispatcher verifier,
                                ITransactionStatusService transactionStatusService)
    {
        _options = options.Value;
        _eventStore = localEventStore;
        _verifier = verifier;
        _transactionStatusService = transactionStatusService;
    }

    public Task Run(JobContext<TransactionJob> context)
    {
        return ProcessTransaction(context.Job.Transaction);
    }

    private async Task ProcessTransaction(V1.Transaction transaction)
    {
        try
        {
            if (transaction.Header.FederatedStreamId.Registry != _options.RegistryName)
                throw new InvalidTransactionException("Invalid registry for transaction");

            var streamId = Guid.Parse(transaction.Header.FederatedStreamId.StreamId.Value);
            var stream = (await _eventStore.GetEventsForEventStream(streamId))
                .Select(x => V1.Transaction.Parser.ParseFrom(x.Content));

            var result = await _verifier.VerifyTransaction(transaction, stream);

            if (!result.Valid)
                throw new InvalidTransactionException(result.ErrorMessage);

            var nextEventIndex = stream.Count();
            var eventId = new EventId(streamId, nextEventIndex);
            var verifiableEvent = new VerifiableEvent(eventId, transaction.ToByteArray());
            await _eventStore.Store(verifiableEvent);
        }
        catch (InvalidTransactionException ex)
        {
            await _transactionStatusService.SetTransactionStatus(transaction.GetTransactionId(), new V1.Internal.TransactionStatus
            {
                State = V1.TransactionState.Failed,
                Message = ex.Message
            });
        }
    }
}
