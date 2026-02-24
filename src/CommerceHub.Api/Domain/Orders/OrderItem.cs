namespace CommerceHub.Api.Domain.Orders;

public sealed class OrderItem
{
    public string ProductId { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}