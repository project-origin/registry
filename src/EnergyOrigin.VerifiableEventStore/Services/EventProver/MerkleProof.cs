namespace EnergyOrigin.VerifiableEventStore.Services.EventProver;

public record MerkleProof(Guid EventId, byte[] Event, string BlockId, string TransactionId, Int64 leafIndex, IEnumerable<byte[]> Hashes);
