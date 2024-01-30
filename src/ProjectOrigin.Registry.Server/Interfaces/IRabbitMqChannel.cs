using System;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace ProjectOrigin.Registry.Server.Interfaces;

public interface IRabbitMqChannel : IDisposable
{
    Task PublishToQueue(string queue, byte[] bytes);
    IModel Channel { get; }
}
