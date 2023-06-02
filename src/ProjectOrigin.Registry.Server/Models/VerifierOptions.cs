using System.Collections.Generic;

namespace ProjectOrigin.Registry.Server.Models;

public class VerifierOptions
{
    public Dictionary<string, string> Verifiers { get; set; } = new Dictionary<string, string>();
}
