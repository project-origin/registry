using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Google.Protobuf;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.Registry.Server.Exceptions;
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
        V1.Transaction transaction = context.Job.ToTransaction();
        var transactionHash = Convert.ToBase64String(SHA256.HashData(transaction.ToByteArray()));
        try
        {
            _logger.LogTrace($"Processing transaction {transactionHash}");

            if (transaction.Header.FederatedStreamId.Registry != _options.RegistryName)
                throw new InvalidTransactionException("Invalid registry for transaction");

            var streamId = Guid.Parse(transaction.Header.FederatedStreamId.StreamId.Value);
            var stream = (await _eventStore.GetEventsForEventStream(streamId))
                .Select(x => V1.Transaction.Parser.ParseFrom(x.Content))
                .ToList();

            var result = await _verifier.VerifyTransaction(transaction, stream);

            if (!result.Valid)
                throw new InvalidTransactionException(result.ErrorMessage);

            var nextEventIndex = stream.Count();
            var eventId = new VerifiableEventStore.Models.EventId(streamId, nextEventIndex);
            var verifiableEvent = new VerifiableEvent(eventId, transactionHash, transaction.ToByteArray());
            await _eventStore.Store(verifiableEvent);

            _logger.LogTrace($"Transaction processed {transactionHash}");
        }
        catch (InvalidTransactionException ex)
        {
            _logger.LogWarning(ex, $"Invalid transaction {transactionHash} - {ex.Message}");
            await _transactionStatusService.SetTransactionStatus(
                transactionHash,
                new TransactionStatusRecord(
                TransactionStatus.Failed,
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unknown exception for transaction {transactionHash} -  {ex.Message}");
            throw;
        }
    }
}
