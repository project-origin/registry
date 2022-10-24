using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.VerifiableEventStore.Services.EventProver;

public record MerkleProof(EventId EventId, byte[] Event, string BlockId, string TransactionId, long leafIndex, IEnumerable<byte[]> Hashes);
