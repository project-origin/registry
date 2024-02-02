using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ProjectOrigin.Registry.Server.Options;

public record TransactionProcessorOptions()
{
    // To compensate for pre 1.28 kubernetes that does not support apps.kubernetes.io/pod-index
    public string? PodName
    {
        init
        {
            // regex to find last integer in a string
            if (value is not null)
            {
                var match = Regex.Match(value, @"\d+$");
                if (match.Success)
                {
                    ServerNumber = int.Parse(match.Value);
                }
            }
        }
    }

    [Required, Range(0, 127)]
    public required int ServerNumber { get; init; }

    [Required, Range(1, 128)]
    public required int Servers { get; init; }

    [Required, Range(1, 128)]
    public required int Threads { get; init; }

    [Required, Range(1, 100)]
    public required int Weight { get; init; } = 10;
}
