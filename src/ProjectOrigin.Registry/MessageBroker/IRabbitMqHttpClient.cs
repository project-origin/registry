using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectOrigin.Registry.MessageBroker;

public interface IRabbitMqHttpClient
{
    Task<IEnumerable<RabbitMqQueue>> GetQueuesAsync();
}
