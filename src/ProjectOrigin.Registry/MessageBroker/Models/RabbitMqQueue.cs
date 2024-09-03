namespace ProjectOrigin.Registry.Server.Models;

public record RabbitMqQueue()
{
    public required string Name { get; init; }
    public required string Vhost { get; init; }
    public required string Type { get; init; }
    public required string Node { get; init; }
    public required string State { get; init; }
    public required bool AutoDelete { get; init; }
    public required bool Durable { get; init; }
    public required bool Exclusive { get; init; }
    public required int Messages { get; init; }
    public required int MessagesReady { get; init; }
    public required int MessagesUnacknowledged { get; init; }
}
