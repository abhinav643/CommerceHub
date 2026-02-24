namespace CommerceHub.Api.Dtos;

public sealed class CheckoutRequest
{
    public string CustomerId { get; set; } = default!;
    public List<CheckoutItemDto> Items { get; set; } = new();
}

public sealed class CheckoutItemDto
{
    public string ProductId { get; set; } = default!;
    public int Quantity { get; set; }
}