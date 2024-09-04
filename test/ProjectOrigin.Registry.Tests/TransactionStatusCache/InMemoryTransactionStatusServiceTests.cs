using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MsOptions = Microsoft.Extensions.Options.Options;
using Moq;
using ProjectOrigin.Registry.TransactionStatusCache;

namespace ProjectOrigin.Registry.Tests.TransactionStatusCache;

public class InMemoryTransactionStatusServiceTests : AbstractTransactionStatusServiceTests
{
    private InMemoryTransactionStatusService _service;

    public InMemoryTransactionStatusServiceTests()
    {
        var cache = new MemoryDistributedCache(MsOptions.Create(new MemoryDistributedCacheOptions()));
        var logger = new Mock<ILogger<InMemoryTransactionStatusService>>();

        _service = new InMemoryTransactionStatusService(logger.Object, cache, _repository);
    }

    protected override ITransactionStatusService Service => _service;
}
