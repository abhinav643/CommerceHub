using CommerceHub.Api.Domain.Orders;

namespace CommerceHub.Api.Dtos;

public sealed class ReplaceOrderRequest
{
    public string CustomerId { get; set; } = default!;
    public List<ReplaceOrderItemDto> Items { get; set; } = new();

    // Caller can replace status too, but we will block if already shipped
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
}

public sealed class ReplaceOrderItemDto
{
    public string ProductId { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}