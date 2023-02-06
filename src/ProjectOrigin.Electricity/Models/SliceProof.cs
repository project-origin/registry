using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Models;

internal record SliceProof(SecretCommitmentInfo Source, SecretCommitmentInfo Quantity, SecretCommitmentInfo Remainder);
