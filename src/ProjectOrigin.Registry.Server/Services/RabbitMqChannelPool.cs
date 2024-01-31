using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using ProjectOrigin.Registry.Server.Interfaces;
using ProjectOrigin.Registry.Server.Options;
using RabbitMQ.Client;

namespace ProjectOrigin.Registry.Server.Services;

public sealed class RabbitMqChannelPool : IRabbitMqChannelPool
{
    private readonly Lazy<IConnection> _connection;
    private readonly ConcurrentBag<IChannel> _channels;
    private readonly ConcurrentBag<IChannel> _availableChannels;

    public RabbitMqChannelPool(IOptions<RabbitMqOptions> rabbitMqOptions)
    {
        var factory = new ConnectionFactory()
        {
            HostName = rabbitMqOptions.Value.Hostname,
            Port = rabbitMqOptions.Value.AmqpPort,
            UserName = rabbitMqOptions.Value.Username,
            Password = rabbitMqOptions.Value.Password,
            DispatchConsumersAsync = true,
        };
        _connection = new Lazy<IConnection>(() => factory.CreateConnection());
        _channels = new ConcurrentBag<IChannel>();
        _availableChannels = new ConcurrentBag<IChannel>();
    }

    public void Dispose()
    {
        foreach (var channel in _channels)
        {
            channel.Dispose();
        }

        if (_connection.IsValueCreated)
            _connection.Value.Dispose();
    }

    public IRabbitMqChannel GetChannel()
    {
        if (_availableChannels.TryTake(out var channel))
        {
            return new ChannelWrapper(channel, this);
        }
        else
        {
            var newChannel = _connection.Value.CreateChannel();
            _channels.Add(newChannel);
            return new ChannelWrapper(newChannel, this);
        }
    }

    public void ReturnChannel(IChannel channel)
    {
        _availableChannels.Add(channel);
    }

    private sealed class ChannelWrapper : IRabbitMqChannel
    {
        private readonly RabbitMqChannelPool _channelPool;
        private readonly IChannel _channel;

        public ChannelWrapper(IChannel channel, RabbitMqChannelPool channelPool)
        {
            _channel = channel;
            _channelPool = channelPool;
        }

        public IChannel Channel => _channel;

        public void Dispose()
        {
            _channelPool.ReturnChannel(_channel);
        }
    }
}
