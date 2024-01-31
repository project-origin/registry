using System.ComponentModel.DataAnnotations;

namespace ProjectOrigin.Registry.Server.Options;

public record RabbitMqOptions
{
    [Required]
    public required string Hostname { get; init; }

    [Required, Range(1, 65535)]
    public required int AmqpPort { get; init; }

    [Required, Range(1, 65535)]
    public required int HttpApiPort { get; init; }

    [Required]
    public required string Username { get; init; }

    [Required]
    public required string Password { get; init; }
}
