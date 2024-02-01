using System.ComponentModel.DataAnnotations;

namespace ProjectOrigin.Registry.Server.Options;

public record TransactionProcessorOptions()
{
    [Required, Range(0, 127)]
    public required int ServerNumber { get; init; }

    [Required, Range(1, 128)]
    public required int Servers { get; init; }

    [Required, Range(1, 128)]
    public required int Threads { get; init; }

    [Required, Range(1, 100)]
    public required int Weight { get; init; } = 10;
}
