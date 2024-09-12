using System.Threading.Tasks;
using ProjectOrigin.ServiceCommon.Database;

namespace ProjectOrigin.Registry.Repository.InMemory;

public class InMemoryUpgrader : IDatabaseUpgrader
{
    public Task<bool> IsUpgradeRequired()
    {
        return Task.FromResult(false);
    }

    public Task Upgrade() => Task.CompletedTask;
}
