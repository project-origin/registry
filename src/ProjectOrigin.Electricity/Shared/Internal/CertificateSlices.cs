using NSec.Cryptography;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Shared.Internal;

internal record CertificateSlice(Commitment Commitment, PublicKey Owner);

internal record AllocationSlice(Commitment Commitment, PublicKey Owner, Guid AllocationId, FederatedStreamId ProductionCertificateId, FederatedStreamId ConsumptionCertificateId) : CertificateSlice(Commitment, Owner);
