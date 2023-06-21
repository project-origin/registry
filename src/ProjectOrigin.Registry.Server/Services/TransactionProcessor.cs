using System;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.Registry.Server.Exceptions;
using ProjectOrigin.Registry.Server.Extensions;
using ProjectOrigin.Registry.Server.Interfaces;
using ProjectOrigin.Registry.Server.Models;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;
using ProjectOrigin.VerifiableEventStore.Services.TransactionStatusCache;

namespace ProjectOrigin.Registry.Server.Services;

public class TransactionProcessor : IJobConsumer<TransactionJob>
{
    private TransactionProcessorOptions _options;
    private IEventStore _eventStore;
    private ITransactionDispatcher _verifier;
    private ITransactionStatusService _transactionStatusService;
    private ILogger<TransactionProcessor> _logger;

    public TransactionProcessor(IOptions<TransactionProcessorOptions> options,
                                IEventStore localEventStore,
                                ITransactionDispatcher verifier,
                                ITransactionStatusService transactionStatusService,
                                ILogger<TransactionProcessor> logger)
    {
        _options = options.Value;
        _eventStore = localEventStore;
        _verifier = verifier;
        _transactionStatusService = transactionStatusService;
        _logger = logger;
    }

    public async Task Run(JobContext<TransactionJob> context)
    {
        await ProcessTransaction(context.Job.ToTransaction());
        await context.NotifyCompleted();
    }

    private async Task ProcessTransaction(V1.Transaction transaction)
    {
        try
        {
            _logger.LogInformation($"Processing transaction {transaction.GetTransactionId()}");

            if (transaction.Header.FederatedStreamId.Registry != _options.RegistryName)
                throw new InvalidTransactionException("Invalid registry for transaction");

            var streamId = Guid.Parse(transaction.Header.FederatedStreamId.StreamId.Value);
            var stream = (await _eventStore.GetEventsForEventStream(streamId))
                .Select(x => V1.Transaction.Parser.ParseFrom(x.Content));

            var result = await _verifier.VerifyTransaction(transaction, stream);

            if (!result.Valid)
                throw new InvalidTransactionException(result.ErrorMessage);

            var nextEventIndex = stream.Count();
            var eventId = new VerifiableEventStore.Models.EventId(streamId, nextEventIndex);
            var verifiableEvent = new VerifiableEvent(eventId, transaction.GetTransactionId(), transaction.ToByteArray());
            await _eventStore.Store(verifiableEvent);

            _logger.LogInformation($"Completed transaction {transaction.GetTransactionId()}");
        }
        catch (InvalidTransactionException ex)
        {
            _logger.LogWarning(ex, $"Invalid transaction {transaction.GetTransactionId()} - {ex.Message}");

            await _transactionStatusService.SetTransactionStatus(
                transaction.GetTransactionId(),
                new TransactionStatusRecord(
                TransactionStatus.Failed,
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unknown exction for transaction {transaction.GetTransactionId()} -  {ex.Message}");
            throw;
        }
    }
}
