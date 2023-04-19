using Microsoft.Extensions.Logging;

namespace ProjectOrigin.VerifiableEventStore.Services.BlockchainConnector;

/// The implementation can be used locally to see the output in the log instead of
/// having to send it to
public class LogBlockchainConnector : IBlockchainConnector
{
    private ILogger<LogBlockchainConnector> _logger;

    private int _transactionNumber = 0;

    public LogBlockchainConnector(ILogger<LogBlockchainConnector> logger)
    {
        _logger = logger;
    }

    public Task<Block?> GetBlock(TransactionReference transactionId)
    {
        return Task.FromResult<Block?>(new Block(transactionId.TransactionHash, true));
    }

    public Task<TransactionReference> PublishBytes(byte[] bytes)
    {
        var number = ++_transactionNumber;
        _logger.LogInformation($"Publish transaction {number} bytes: ”{Convert.ToBase64String(bytes)}”");
        return Task.FromResult(new TransactionReference($"{number}"));
    }
}
