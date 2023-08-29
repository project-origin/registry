using ProjectOrigin.VerifiableEventStore.Services.Repository;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore.Memory;

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
