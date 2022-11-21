using NSec.Cryptography;

namespace ProjectOrigin.Electricity.Models;

public record IssuerOptions(Func<string, PublicKey?> AreaIssuerPublicKey);
