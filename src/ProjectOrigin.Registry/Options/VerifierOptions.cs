using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectOrigin.Registry.Server.Options;

public class VerifierOptions()
{
    [Required, MinLength(1)]
    public Dictionary<string, string> Verifiers { get; set; } = new Dictionary<string, string>();
}
