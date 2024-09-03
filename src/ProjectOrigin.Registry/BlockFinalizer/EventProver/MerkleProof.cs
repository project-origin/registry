using System.Collections.Generic;
using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.VerifiableEventStore.Services.EventProver;

public record MerkleProof(TransactionHash TransactionHash, byte[] Transaction, int LeafIndex, IEnumerable<byte[]> Hashes);
