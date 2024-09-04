using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;

namespace ProjectOrigin.Registry.Options;

public partial record TransactionProcessorOptions() : IValidatableObject
{
    // To compensate for pre 1.28 kubernetes that does not support apps.kubernetes.io/pod-index
    [GeneratedRegex(@"\d+$", RegexOptions.Compiled, 100)]
    private static partial Regex EndingNumberRegex();
    private string? _podName = null;
    public string? PodName
    {
        get => _podName;
        init
        {
            _podName = value;
            // regex to find last integer in a string
            if (value is not null && ServerNumber == -1)
            {
                var match = EndingNumberRegex().Match(value);
                if (match.Success)
                {
                    ServerNumber = int.Parse(match.Value);
                }
            }
        }
    }

    [Required, Range(0, 127)]
    public required int ServerNumber { get; init; } = -1;

    [Required, Range(1, 128)]
    public required int Servers { get; init; }

    [Required, Range(1, 128)]
    public required int Threads { get; init; }

    [Required, Range(1, 100)]
    public required int Weight { get; init; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // no additional validation needed
        return Enumerable.Empty<ValidationResult>();
    }
}
