namespace EnergyOrigin.VerifiableEventStore.Api.Services.EventProver;

public interface IEventProver
{
    Task<MerkleProof?> GetMerkleProof(Guid eventId);
}
