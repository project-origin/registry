using ProjectOrigin.VerifiableEventStore.Services.EventStore.InMemory;

namespace ProjectOrigin.VerifiableEventStore.Tests.Repository;

public class InMemoryRepositoryTests : AbstractTransactionRepositoryTests<InMemoryRepository>
{
    private InMemoryRepository _memoryEventStore;

    public InMemoryRepositoryTests()
    {
        _memoryEventStore = new InMemoryRepository();
    }

    protected override InMemoryRepository Repository => _memoryEventStore;
}
