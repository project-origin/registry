using ProjectOrigin.VerifiableEventStore.Services.EventStore.Memory;

namespace ProjectOrigin.VerifiableEventStore.Tests;

public class MemoryEventStoreTests : AbstractEventStoreTests<InMemoryRepository>
{
    private InMemoryRepository _memoryEventStore;

    public MemoryEventStoreTests()
    {
        _memoryEventStore = new InMemoryRepository();
    }

    protected override InMemoryRepository Repository => _memoryEventStore;
}
