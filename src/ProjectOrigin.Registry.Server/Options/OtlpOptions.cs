using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectOrigin.Registry.Server.Options;

public record OtlpOptions : IValidatableObject
{
    public const string Prefix = "Otlp";

    [Required]
    public Uri? Endpoint { get; init; }

    [Required]
    public required bool Enabled { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        switch (Enabled)
        {
            case true when Endpoint == null:
                yield return new ValidationResult(
                    $"The {nameof(Endpoint)} field is required when telemetry is enabled.",
                    new[] { nameof(Endpoint) });
                break;
            case true:
                {
                    if (Endpoint.Scheme != Uri.UriSchemeHttp && Endpoint.Scheme != Uri.UriSchemeHttps)
                    {
                        yield return new ValidationResult(
                            $"The {nameof(Endpoint)} must use the HTTP or HTTPS scheme.",
                            new[] { nameof(Endpoint) });
                    }

                    break;
                }
        }
    }
}
