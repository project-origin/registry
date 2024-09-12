using System.Collections.Generic;
using ProjectOrigin.Registry.Repository.Models;

namespace ProjectOrigin.Registry.Grpc.EventProver;

public record MerkleProof(TransactionHash TransactionHash, byte[] Transaction, int LeafIndex, IEnumerable<byte[]> Hashes);
