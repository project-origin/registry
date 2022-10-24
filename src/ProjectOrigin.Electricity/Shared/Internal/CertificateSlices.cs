using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Shared.Internal;

internal record CertificateSlices(Commitment Commitment, byte[] Owner);
