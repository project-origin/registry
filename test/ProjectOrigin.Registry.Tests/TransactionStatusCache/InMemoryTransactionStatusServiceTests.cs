using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MsOptions = Microsoft.Extensions.Options.Options;
using Moq;
using ProjectOrigin.Registry.TransactionStatusCache;

namespace ProjectOrigin.Registry.Tests.TransactionStatusCache;

public class InMemoryTransactionStatusServiceTests : AbstractTransactionStatusServiceTests
{
    private readonly Mock<ILogger<InMemoryTransactionStatusService>> _mockLogger;
    private readonly InMemoryTransactionStatusService _service;

    public InMemoryTransactionStatusServiceTests()
    {
        var cache = new MemoryDistributedCache(MsOptions.Create(new MemoryDistributedCacheOptions()));
        _mockLogger = new Mock<ILogger<InMemoryTransactionStatusService>>();

        _service = new InMemoryTransactionStatusService(_mockLogger.Object, cache, _repository);
    }

    protected override ITransactionStatusService Service => _service;
    protected override IInvocationList LoggedMessages => _mockLogger.Invocations;
}
