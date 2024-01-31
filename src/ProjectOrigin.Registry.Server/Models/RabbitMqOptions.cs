namespace ProjectOrigin.Registry.Server.Models;

public record RabbitMqOptions
{
    public required string Hostname { get; init; }
    public required int AmqpPort { get; init; }
    public required int HttpApiPort { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
}
