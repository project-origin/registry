using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ProjectOrigin.Registry.Server.Interfaces;
using RabbitMQ.Client;

namespace ProjectOrigin.Registry.Server.Services;

public class RabbitMqChannelPool : IRabbitMqChannelPool
{
    private readonly Lazy<IConnection> _connection;
    private readonly ConcurrentBag<IModel> _channels;
    private readonly ConcurrentBag<IModel> _availableChannels;

    public RabbitMqChannelPool(string connectionString)
    {
        var factory = new ConnectionFactory()
        {
            Uri = new Uri(connectionString),
            DispatchConsumersAsync = true,
        };
        _connection = new Lazy<IConnection>(() => factory.CreateConnection());
        _channels = new ConcurrentBag<IModel>();
        _availableChannels = new ConcurrentBag<IModel>();
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
            var newChannel = _connection.Value.CreateModel();
            _channels.Add(newChannel);
            return new ChannelWrapper(newChannel, this);
        }
    }

    public void ReturnChannel(IModel channel)
    {
        _availableChannels.Add(channel);
    }

    private sealed class ChannelWrapper : IRabbitMqChannel
    {
        private readonly RabbitMqChannelPool _channelPool;
        private readonly IModel _channel;

        public ChannelWrapper(IModel channel, RabbitMqChannelPool channelPool)
        {
            _channel = channel;
            _channelPool = channelPool;
        }

        public IModel Channel => _channel;

        public void Dispose()
        {
            _channelPool.ReturnChannel(_channel);
        }

        public Task PublishToQueue(string queue, byte[] bytes)
        {
            _channel.BasicPublish(
                    exchange: "",
                    routingKey: queue,
                    mandatory: false,
                    basicProperties: null,
                    body: bytes
                    );

            IBasicProperties props = _channel.CreateBasicProperties();
            props.Expiration = "60000";


            return Task.CompletedTask;
        }
    }
}
