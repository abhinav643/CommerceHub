namespace CommerceHub.Api.Infrastructure.Messaging.Events;

public class OrderCreatedEvent
{
    public string OrderId { get; set; } = default!;
    public string CustomerId { get; set; } = default!;
    public decimal Total { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}