namespace EnergyOrigin.VerifiableEventStore.Services.EventProver;

public interface IEventProver
{
    Task<MerkleProof?> GetMerkleProof(Guid eventId);
}
