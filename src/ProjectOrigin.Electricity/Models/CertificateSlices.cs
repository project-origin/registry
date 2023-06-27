namespace ProjectOrigin.Electricity.Models;

public record AllocationSlice(
    Electricity.V1.Commitment Commitment,
    Electricity.V1.PublicKey Owner,
    Common.V1.Uuid AllocationId,
    Common.V1.FederatedStreamId ProductionCertificateId,
    Common.V1.FederatedStreamId ConsumptionCertificateId) : CertificateSlice(Commitment, Owner);

public record CertificateSlice(
    Electricity.V1.Commitment Commitment,
    Electricity.V1.PublicKey Owner);
