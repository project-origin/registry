using System;
using System.Linq;
using System.Security.Cryptography;
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
using ProjectOrigin.VerifiableEventStore.Services.Repository;
using ProjectOrigin.VerifiableEventStore.Services.TransactionStatusCache;

namespace ProjectOrigin.Registry.Server.Services;

public class VerifyTransactionConsumer : IConsumer<VerifyTransaction>
{
    private TransactionProcessorOptions _options;
    private ITransactionRepository _transactionRepository;
    private ITransactionDispatcher _verifier;
    private ITransactionStatusService _transactionStatusService;
    private ILogger<VerifyTransactionConsumer> _logger;

    public VerifyTransactionConsumer(IOptions<TransactionProcessorOptions> options,
                                ITransactionRepository transactionRepository,
                                ITransactionDispatcher verifier,
                                ITransactionStatusService transactionStatusService,
                                ILogger<VerifyTransactionConsumer> logger)
    {
        _options = options.Value;
        _transactionRepository = transactionRepository;
        _verifier = verifier;
        _transactionStatusService = transactionStatusService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<VerifyTransaction> context)
    {
        V1.Transaction transaction = context.Message.ToTransaction();
        var transactionHash = transaction.GetTransactionHash();
        try
        {
            _logger.LogDebug($"Processing transaction {transactionHash}");

            if (transaction.Header.FederatedStreamId.Registry != _options.RegistryName)
                throw new InvalidTransactionException("Invalid registry for transaction");

            var streamId = Guid.Parse(transaction.Header.FederatedStreamId.StreamId.Value);
            var stream = (await _transactionRepository.GetStreamTransactionsForStream(streamId).ConfigureAwait(false))
                .Select(x => V1.Transaction.Parser.ParseFrom(x.Payload))
                .ToList();

            var result = await _verifier.VerifyTransaction(transaction, stream).ConfigureAwait(false);

            if (!result.Valid)
                throw new InvalidTransactionException(result.ErrorMessage);

            var nextEventIndex = stream.Count;
            var verifiableEvent = new StreamTransaction { TransactionHash = transactionHash, StreamId = streamId, StreamIndex = nextEventIndex, Payload = transaction.ToByteArray() };
            await _transactionRepository.Store(verifiableEvent).ConfigureAwait(false);

            _logger.LogDebug($"Transaction processed {transactionHash}");
        }
        catch (InvalidTransactionException ex)
        {
            _logger.LogWarning(ex, "Invalid transaction {transactionHash} - {exceptionMessage}", transactionHash, ex.Message);
            await _transactionStatusService.SetTransactionStatus(
                transactionHash,
                new TransactionStatusRecord(
                TransactionStatus.Failed,
                ex.Message)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unknown exception for transaction {transactionHash} -  {exceptionMessage}", transactionHash, ex.Message);
            throw;
        }
    }
}

public class VerifyTransactionConsumerDefinition : ConsumerDefinition<VerifyTransactionConsumer>
{
    public VerifyTransactionConsumerDefinition()
    {
        // limit the number of messages consumed concurrently
        // this applies to the consumer only, not the endpoint
        ConcurrentMessageLimit = 1;
    }
}
