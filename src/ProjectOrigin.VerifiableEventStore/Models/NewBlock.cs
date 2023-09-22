using System.Collections.Generic;

namespace ProjectOrigin.VerifiableEventStore.Models;

public record NewBlock(ImmutableLog.V1.BlockHeader Header, IReadOnlyList<TransactionHash> TransactionHashes);
