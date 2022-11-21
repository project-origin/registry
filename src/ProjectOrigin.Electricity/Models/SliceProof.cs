using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Models;

public record SliceProof(CommitmentParameters Source, CommitmentParameters Quantity, CommitmentParameters Remainder);
