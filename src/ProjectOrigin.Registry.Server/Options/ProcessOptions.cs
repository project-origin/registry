using System.ComponentModel.DataAnnotations;

namespace ProjectOrigin.Registry.Server.Options;

public record ProcessOptions
{
    [Required, Range(0, 127)]
    public int ServerNumber { get; init; }

    [Required, Range(1, 128)]
    public int Servers { get; init; }

    [Required, Range(1, 128)]
    public int VerifyThreads { get; init; }

    [Required, Range(1, 100)]
    public int Weight { get; init; } = 10;
}
