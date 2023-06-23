using System.Collections.Generic;

namespace ProjectOrigin.VerifiableEventStore.Services.EventProver;

public record MerkleProof(string transactionId, byte[] Event, string BlockId, string TransactionId, long leafIndex, IEnumerable<byte[]> Hashes);
