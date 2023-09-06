using System.Threading.Tasks;
using ProjectOrigin.VerifiableEventStore.Services.Repository;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore.InMemory;

public class InMemoryUpgrader : IRepositoryUpgrader
{
    public Task<bool> IsUpgradeRequired()
    {
        return Task.FromResult(false);
    }

    public Task Upgrade() => Task.CompletedTask;
}
