using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ProjectOrigin.VerifiableEventStore.Services.TransactionStatusCache;

namespace ProjectOrigin.VerifiableEventStore.Tests.TransactionStatusCache;

public class InMemoryTransactionStatusServiceTests : AbstractTransactionStatusServiceTests
{
    private InMemoryTransactionStatusService _service;

    public InMemoryTransactionStatusServiceTests()
    {
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var logger = new Mock<ILogger<InMemoryTransactionStatusService>>();

        _service = new InMemoryTransactionStatusService(logger.Object, cache, _repository);
    }

    protected override ITransactionStatusService Service => _service;
}
