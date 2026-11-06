using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SentinelFlow.Infrastructure;

public interface IMessageBus
{
    Task PublishAsync<T>(string routingKey, T message, CancellationToken ct = default);
    Task<IAsyncDisposable> SubscribeAsync<T>(string queue, string routingKey, Func<T, CancellationToken, Task> handler, CancellationToken ct = default);
}

public class RabbitMqBus : IMessageBus, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    public const string Exchange = "sentinelflow.events";

    public RabbitMqBus(string host = "localhost", string user = "guest", string password = "guest")
    {
        var factory = new ConnectionFactory { HostName = host, UserName = user, Password = password };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        _channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true).GetAwaiter().GetResult();
    }

    public async Task PublishAsync<T>(string routingKey, T message, CancellationToken ct = default)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var props = new BasicProperties { Persistent = true, ContentType = "application/json" };
        await _channel.BasicPublishAsync(Exchange, routingKey, mandatory: false, props, body, ct);
    }

    public async Task<IAsyncDisposable> SubscribeAsync<T>(string queue, string routingKey, Func<T, CancellationToken, Task> handler, CancellationToken ct = default)
    {
        await _channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false);
        await _channel.QueueBindAsync(queue, Exchange, routingKey);
        var consumer = new AsyncEventingBasicConsumer(_channel);
        var tcs = new TaskCompletionSource();
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var msg = JsonSerializer.Deserialize<T>(ea.Body.ToArray());
                if (msg is not null) await handler(msg, ct);
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch
            {
                await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true);
            }
        };
        await _channel.BasicConsumeAsync(queue, autoAck: false, consumer);
        return new Disposable(async () =>
        {
            try { await _channel.QueueDeleteAsync(queue); } catch { }
            tcs.TrySetResult();
        });
    }

    private sealed class Disposable(Func<Task> dispose) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => new(dispose());
    }

    public async ValueTask DisposeAsync()
    {
        try { await _channel.CloseAsync(); } catch { }
        try { await _connection.CloseAsync(); } catch { }
        _channel.Dispose();
        _connection.Dispose();
    }
}
