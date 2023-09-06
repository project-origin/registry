
namespace ProjectOrigin.VerifiableEventStore.Services.Repository;

public interface IRepositoryUpgrader
{
    void Upgrade();
    bool IsUpgradeRequired();
}
