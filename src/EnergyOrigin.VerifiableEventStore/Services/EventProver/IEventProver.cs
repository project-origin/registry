using EnergyOrigin.VerifiableEventStore.Models;

namespace EnergyOrigin.VerifiableEventStore.Services.EventProver;

public interface IEventProver
{
    Task<MerkleProof?> GetMerkleProof(EventId eventId);
}
