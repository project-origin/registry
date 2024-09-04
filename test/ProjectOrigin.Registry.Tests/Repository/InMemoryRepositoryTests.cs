using ProjectOrigin.Registry.Repository.InMemory;

namespace ProjectOrigin.Registry.Tests.Repository;

public class InMemoryRepositoryTests : AbstractTransactionRepositoryTests<InMemoryRepository>
{
    private InMemoryRepository _memoryEventStore;

    public InMemoryRepositoryTests()
    {
        _memoryEventStore = new InMemoryRepository();
    }

    protected override InMemoryRepository Repository => _memoryEventStore;
}
