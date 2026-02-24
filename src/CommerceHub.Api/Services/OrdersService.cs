using CommerceHub.Api.Domain.Orders;
using CommerceHub.Api.Dtos;
using CommerceHub.Api.Repositories;

namespace CommerceHub.Api.Services;

public sealed class OrdersService
{
    private readonly IOrdersRepository _orders;

    public OrdersService(IOrdersRepository orders)
    {
        _orders = orders;
    }

    public Task<Order?> GetByIdAsync(string id, CancellationToken ct)
        => _orders.GetByIdAsync(id, ct);

    public async Task<(bool Success, string? Error, bool NotFound, bool ShippedBlocked)> ReplaceAsync(
        string id,
        ReplaceOrderRequest req,
        CancellationToken ct)
    {
        if (req is null) return (false, "Request body is required.", false, false);
        if (string.IsNullOrWhiteSpace(req.CustomerId)) return (false, "CustomerId is required.", false, false);
        if (req.Items is null || req.Items.Count == 0) return (false, "At least one item is required.", false, false);

        foreach (var item in req.Items)
        {
            if (string.IsNullOrWhiteSpace(item.ProductId)) return (false, "ProductId is required.", false, false);
            if (item.Quantity <= 0) return (false, "Quantity must be greater than 0.", false, false);
            if (item.UnitPrice < 0) return (false, "UnitPrice cannot be negative.", false, false);
        }

        var existing = await _orders.GetByIdAsync(id, ct);
        if (existing is null) return (false, "Order not found.", true, false);
        if (existing.Status == OrderStatus.Shipped) return (false, "Order cannot be updated once shipped.", false, true);

        var replacement = new Order
        {
            CustomerId = req.CustomerId,
            Status = req.Status,
            Items = req.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList(),
            CreatedAtUtc = existing.CreatedAtUtc,
            UpdatedAtUtc = DateTime.UtcNow
        };

        replacement.Total = replacement.Items.Sum(i => i.UnitPrice * i.Quantity);

        var ok = await _orders.ReplaceIfNotShippedAsync(id, replacement, ct);
        return ok ? (true, null, false, false) : (false, "Order not found or already shipped.", false, false);
    }
}