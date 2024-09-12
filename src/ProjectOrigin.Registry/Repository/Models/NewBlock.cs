using System.Collections.Generic;

namespace ProjectOrigin.Registry.Repository.Models;

public record NewBlock(Registry.V1.BlockHeader Header, IReadOnlyList<TransactionHash> TransactionHashes);
