using ProjectOrigin.VerifiableEventStore.Services.Repository;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore.InMemory;

public class InMemoryUpgrader : IRepositoryUpgrader
{
    public bool IsUpgradeRequired()
    {
        return false;
    }

    public void Upgrade()
    {
    }
}
