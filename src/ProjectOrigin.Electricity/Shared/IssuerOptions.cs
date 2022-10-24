using NSec.Cryptography;

namespace ProjectOrigin.Electricity.Shared.Internal;

public record IssuerOptions(Func<string, PublicKey?> AreaIssuerPublicKey);
