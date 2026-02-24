using System.Text;
using System.Text.Json;
using CommerceHub.Api.Infrastructure.Messaging.Events;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace CommerceHub.Api.Infrastructure.Messaging;

public sealed class RabbitPublisher : IRabbitPublisher, IDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    public RabbitPublisher(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            UserName = _options.UserName,
            Password = _options.Password
        };

        // v7 is async-first; for DI simplicity we sync-wait here.
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.ExchangeDeclareAsync(
            exchange: _options.Exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null
        ).GetAwaiter().GetResult();
    }

    public async Task PublishOrderCreatedAsync(OrderCreatedEvent evt, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(evt);
        var body = Encoding.UTF8.GetBytes(json);

        // In v7, create a new BasicProperties instance (CreateBasicProperties was removed).
        var props = new BasicProperties
        {
            Persistent = true
        };

        await _channel.BasicPublishAsync(
            exchange: _options.Exchange,
            routingKey: _options.RoutingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct
        );
    }

    public void Dispose()
    {
        try { _channel?.Dispose(); } catch { }
        try { _connection?.Dispose(); } catch { }
    }
}