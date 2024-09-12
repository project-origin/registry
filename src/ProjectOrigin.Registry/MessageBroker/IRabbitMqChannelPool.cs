using System.Threading.Tasks;

namespace ProjectOrigin.Registry.MessageBroker;

public interface IRabbitMqChannelPool
{
    Task<IRabbitMqChannel> GetChannelAsync();
}
