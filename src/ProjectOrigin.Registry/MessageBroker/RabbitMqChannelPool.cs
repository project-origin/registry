using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ProjectOrigin.Registry.Server.Interfaces;
using ProjectOrigin.Registry.Server.Options;
using RabbitMQ.Client;

namespace ProjectOrigin.Registry.Server.Services;

public sealed class RabbitMqChannelPool : IRabbitMqChannelPool, IAsyncDisposable
{
    private readonly Lazy<Task<IConnection>> _connection;
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

        _connection = new Lazy<Task<IConnection>>(() => Task.Run(() => factory.CreateConnectionAsync()), false);
        _channels = new ConcurrentBag<IChannel>();
        _availableChannels = new ConcurrentBag<IChannel>();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var channel in _channels)
        {
            await channel.CloseAsync();
            channel.Dispose();
        }

        if (_connection.IsValueCreated)
        {
            var connection = await _connection.Value;
            await connection.CloseAsync();
            _connection.Value.Dispose();
        }
    }

    public async Task<IRabbitMqChannel> GetChannelAsync()
    {
        if (_availableChannels.TryTake(out var channel))
        {
            return new ChannelWrapper(channel, this);
        }
        else
        {
            var connection = await _connection.Value;
            var newChannel = await connection.CreateChannelAsync();
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
