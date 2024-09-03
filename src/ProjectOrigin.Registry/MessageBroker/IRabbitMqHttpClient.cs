using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectOrigin.Registry.Server.Models;

namespace ProjectOrigin.Registry.Server.Interfaces;

public interface IRabbitMqHttpClient
{
    Task<IEnumerable<RabbitMqQueue>> GetQueuesAsync();
}
