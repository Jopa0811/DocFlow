using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace DocFlow.Messaging;

public class RabbitMqEventPublisher(IChannel channel) : IEventPublisher
{
    public async Task PublishAsync<TEvent>(TEvent @event, string routingKey, CancellationToken cancellationToken = default)
        where TEvent : notnull
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event));

        await channel.BasicPublishAsync(
            exchange: RabbitMqTopology.DokumenteExchange,
            routingKey: routingKey,
            body: body,
            cancellationToken: cancellationToken);
    }
}
