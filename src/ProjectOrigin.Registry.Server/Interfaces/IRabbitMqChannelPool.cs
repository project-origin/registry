using System;

namespace ProjectOrigin.Registry.Server.Interfaces;

public interface IRabbitMqChannelPool : IDisposable
{
    IRabbitMqChannel GetChannel();
}
