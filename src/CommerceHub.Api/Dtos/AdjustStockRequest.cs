namespace CommerceHub.Api.Dtos;

public sealed class AdjustStockRequest
{
    // Positive increases stock, negative decreases stock
    public int Delta { get; set; }
}