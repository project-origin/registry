using System.Collections.Generic;

namespace ProjectOrigin.Electricity.Server.Options;

public class IssuerOptions
{
    public Dictionary<string, string> Issuers { get; set; } = new Dictionary<string, string>();
}
