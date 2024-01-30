using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectOrigin.WalletSystem.Server.Options;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageBrokerType { InMemory, RabbitMq }

public class MessageBrokerOptions : IValidatableObject
{
    public MessageBrokerType Type { get; set; }

    public RabbitMqOptions? RabbitMq { get; set; } = null;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        List<ValidationResult> results = new();
        switch (Type)
        {
            case MessageBrokerType.RabbitMq:
                if (RabbitMq is null)
                    results.Add(new ValidationResult($"Not supported message broker type: ”{Type}”"));
                else
                    Validator.TryValidateObject(RabbitMq, new ValidationContext(RabbitMq), results, true);
                break;

            default:
                results.Add(new ValidationResult($"Not supported message broker type: ”{Type}”"));
                break;
        }

        return results;
    }
}

public class RabbitMqOptions
{
    [Required]
    public string Host { get; set; } = string.Empty;

    [Required, Range(1, 65535)]
    public ushort Port { get; set; } = 0;

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

