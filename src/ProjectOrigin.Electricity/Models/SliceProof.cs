using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Models;

internal record SliceProof(CommitmentParameters Source, CommitmentParameters Quantity, CommitmentParameters Remainder);
