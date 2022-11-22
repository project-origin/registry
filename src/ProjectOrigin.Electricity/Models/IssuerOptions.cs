using NSec.Cryptography;

namespace ProjectOrigin.Electricity.Models;

public record IssuerOptions
{
    public Func<string, PublicKey?> AreaIssuerPublicKey { get; set; } = _ => null;
}
