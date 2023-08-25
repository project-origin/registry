using Microsoft.Extensions.Options;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.EventStore;
using ProjectOrigin.VerifiableEventStore.Services.EventStore.Memory;

namespace ProjectOrigin.VerifiableEventStore.Tests;

public class MemoryEventStoreTests : AbstractEventStoreTests<MemoryEventStore>
{
    private MemoryEventStore _memoryEventStore;

    public MemoryEventStoreTests()
    {
        var calculator = new BlockSizeCalculator(Options.Create(new VerifiableEventStoreOptions()
        {
            MaxExponent = MaxBatchExponent,
        }));

        _memoryEventStore = new MemoryEventStore(calculator);
    }

    protected override MemoryEventStore EventStore => _memoryEventStore;
}
