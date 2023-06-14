using Microsoft.Extensions.Options;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;

namespace ProjectOrigin.VerifiableEventStore.Tests;

public class MemoryEventStoreTests : AbstractEventStoreTests<MemoryEventStore>
{
    private MemoryEventStore _memoryEventStore;

    public MemoryEventStoreTests()
    {
        var options = new VerifiableEventStoreOptions() { BatchSizeExponent = BatchExponent };
        _memoryEventStore = new MemoryEventStore(Options.Create(options));
    }

    protected override MemoryEventStore EventStore => _memoryEventStore;
}
