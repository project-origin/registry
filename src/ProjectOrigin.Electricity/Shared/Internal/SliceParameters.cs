using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Shared.Internal;

internal record SliceParameters(
    CommitmentParameters Source,
    CommitmentParameters Quantity,
    CommitmentParameters Remainder
);
