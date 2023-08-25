using System;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.VerifiableEventStore.Services.BatchPublisher.Log;

/// The implementation can be used locally to see the output in the log instead of
/// having to send it to
public class LogBatchPublisher : IBatchPublisher
{
    private ILogger<LogBatchPublisher> _logger;

    public LogBatchPublisher(ILogger<LogBatchPublisher> logger)
    {
        _logger = logger;
    }

    public Task<ImmutableLog.V1.BlockPublication> PublishBatch(ImmutableLog.V1.BlockHeader batchHeader)
    {
        var hash = BatchHash.FromHeader(batchHeader);
        var hashBase64 = Convert.ToBase64String(hash.Data);
        _logger.LogInformation($"Batch published - {hashBase64}");

        return Task.FromResult(new ImmutableLog.V1.BlockPublication
        {
            LogEntry = new ImmutableLog.V1.BlockPublication.Types.LogEntry
            {
                BatchHeaderHash = ByteString.CopyFrom(hash.Data),
            }
        });
    }
}
