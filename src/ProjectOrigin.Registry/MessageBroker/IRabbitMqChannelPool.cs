using System.Threading.Tasks;

namespace ProjectOrigin.Registry.Server.Interfaces;

public interface IRabbitMqChannelPool
{
    Task<IRabbitMqChannel> GetChannelAsync();
}
