using CommerceHub.Api.Infrastructure.Messaging.Events;

namespace CommerceHub.Api.Infrastructure.Messaging;

public interface IRabbitPublisher
{
    Task PublishOrderCreatedAsync(OrderCreatedEvent evt, CancellationToken ct);
}