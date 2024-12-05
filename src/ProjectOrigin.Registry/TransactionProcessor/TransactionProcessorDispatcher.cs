using System;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.Registry.Exceptions;
using ProjectOrigin.Registry.Extensions;
using ProjectOrigin.Registry.Options;
using ProjectOrigin.Registry.Repository;
using ProjectOrigin.Registry.Repository.Models;
using ProjectOrigin.Registry.TransactionStatusCache;

namespace ProjectOrigin.Registry.TransactionProcessor;

public class TransactionProcessorDispatcher
{
    private readonly RegistryOptions _options;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionDispatcher _verifier;
    private readonly ITransactionStatusService _transactionStatusService;
    private readonly ILogger<TransactionProcessorDispatcher> _logger;

    public TransactionProcessorDispatcher(IOptions<RegistryOptions> options,
                                ITransactionRepository transactionRepository,
                                ITransactionDispatcher verifier,
                                ITransactionStatusService transactionStatusService,
                                ILogger<TransactionProcessorDispatcher> logger)
    {
        _options = options.Value;
        _transactionRepository = transactionRepository;
        _verifier = verifier;
        _transactionStatusService = transactionStatusService;
        _logger = logger;
    }

    public async Task Verify(V1.Transaction transaction)
    {
        var transactionHash = transaction.GetTransactionHash();
        try
        {
            _logger.LogDebug("Processing transaction {transactionHash}", transactionHash);

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
            await _transactionStatusService.SetTransactionStatus(transactionHash, new TransactionStatusRecord(TransactionStatus.Committed));

            _logger.LogDebug("Transaction processed {transactionHash}", transactionHash);
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
            throw new InvalidTransactionException(string.Format("Unknown exception for transaction {0}", transactionHash), ex);
        }
    }
}
