using EnergyOrigin.VerifiableEventStore.Models;

namespace EnergyOrigin.VerifiableEventStore.Services.EventProver;

public record MerkleProof(EventId EventId, byte[] Event, string BlockId, string TransactionId, Int64 leafIndex, IEnumerable<byte[]> Hashes);
