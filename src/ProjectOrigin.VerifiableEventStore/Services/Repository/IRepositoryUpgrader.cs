using System.Threading.Tasks;

namespace ProjectOrigin.VerifiableEventStore.Services.Repository;

public interface IRepositoryUpgrader
{
    Task Upgrade();
    Task<bool> IsUpgradeRequired();
}
