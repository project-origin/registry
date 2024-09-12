using System;
using RabbitMQ.Client;

namespace ProjectOrigin.Registry.MessageBroker;

public interface IRabbitMqChannel : IDisposable
{
    IChannel Channel { get; }
}
