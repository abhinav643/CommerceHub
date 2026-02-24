using CommerceHub.Api.Domain.Orders;
using CommerceHub.Api.Dtos;
using CommerceHub.Api.Infrastructure.Messaging;
using CommerceHub.Api.Infrastructure.Messaging.Events;
using CommerceHub.Api.Repositories;

namespace CommerceHub.Api.Services;

public sealed class CheckoutService
{
    private readonly IProductsRepository _products;
    private readonly IOrdersRepository _orders;
    private readonly IRabbitPublisher _publisher;

    public CheckoutService(IProductsRepository products, IOrdersRepository orders, IRabbitPublisher publisher)
    {
        _products = products;
        _orders = orders;
        _publisher = publisher;
    }

    public async Task<(bool Success, string? Error, Order? Order)> CheckoutAsync(CheckoutRequest req, CancellationToken ct)
    {
        var validationError = Validate(req);
        if (validationError is not null)
            return (false, validationError, null);

        // Track decremented items for compensation if later item fails.
        var decremented = new List<(string ProductId, int Qty)>();

        // 1) Atomic stock decrement per item
        foreach (var item in req.Items)
        {
            var ok = await _products.TryDecrementStockAsync(item.ProductId, item.Quantity, ct);
            if (!ok)
            {
                // Roll back earlier decrements (best-effort)
                foreach (var d in decremented)
                {
                    await _products.ForceIncrementStockAsync(d.ProductId, d.Qty, ct);
                }

                return (false, $"Insufficient stock for product {item.ProductId}.", null);
            }

            decremented.Add((item.ProductId, item.Quantity));
        }

        // 2) Build order items with prices
        var orderItems = new List<OrderItem>();
        decimal total = 0m;

        foreach (var item in req.Items)
        {
            var product = await _products.GetByIdAsync(item.ProductId, ct);
            if (product is null)
            {
                // Rollback if product disappeared / bad id
                foreach (var d in decremented)
                    await _products.ForceIncrementStockAsync(d.ProductId, d.Qty, ct);

                return (false, $"Product not found: {item.ProductId}", null);
            }

            orderItems.Add(new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            });

            total += product.Price * item.Quantity;
        }

        // 3) Create order
        var order = new Order
        {
            CustomerId = req.CustomerId,
            Items = orderItems,
            Total = total,
            Status = OrderStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _orders.InsertAsync(order, ct);

        // 4) Publish OrderCreated event (only after order is persisted)
        var evt = new OrderCreatedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            Total = order.Total,
            CreatedAtUtc = order.CreatedAtUtc
        };

        await _publisher.PublishOrderCreatedAsync(evt, ct);

        return (true, null, order);
    }

    private static string? Validate(CheckoutRequest req)
    {
        if (req is null) return "Request body is required.";
        if (string.IsNullOrWhiteSpace(req.CustomerId)) return "CustomerId is required.";
        if (req.Items is null || req.Items.Count == 0) return "At least one item is required.";

        foreach (var item in req.Items)
        {
            if (string.IsNullOrWhiteSpace(item.ProductId)) return "ProductId is required for all items.";
            if (item.Quantity <= 0) return "Quantity must be greater than 0 for all items.";
            if (item.Quantity < 0) return "Quantity cannot be negative.";
        }

        // Avoid double-decrement surprises
        var dup = req.Items.GroupBy(i => i.ProductId).FirstOrDefault(g => g.Count() > 1);
        if (dup is not null) return $"Duplicate product in items: {dup.Key}";

        return null;
    }
}