using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ProjectOrigin.Registry.Options;

public partial record TransactionProcessorOptions() : IValidatableObject
{
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
