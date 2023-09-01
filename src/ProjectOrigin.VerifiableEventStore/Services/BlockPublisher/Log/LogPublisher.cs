using System;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.VerifiableEventStore.Services.BlockPublisher.Log;

/// The implementation can be used locally to see the output in the log, but it is not recommended for production use.
/// There is no real immutability guarantee, as the log file is not tamper evident.
public class LogPublisher : IBlockPublisher
{
    private ILogger<LogPublisher> _logger;

    public LogPublisher(ILogger<LogPublisher> logger)
    {
        _logger = logger;
    }

    public Task<ImmutableLog.V1.BlockPublication> PublishBlock(ImmutableLog.V1.BlockHeader blockHeader)
    {
        var hash = BlockHash.FromHeader(blockHeader);
        var hashBase64 = Convert.ToBase64String(hash.Data);
        _logger.LogInformation($"Block published - {hashBase64}");

        return Task.FromResult(new ImmutableLog.V1.BlockPublication
        {
            LogEntry = new ImmutableLog.V1.BlockPublication.Types.LogEntry
            {
                BlockHeaderHash = ByteString.CopyFrom(hash.Data),
            }
        });
    }
}
