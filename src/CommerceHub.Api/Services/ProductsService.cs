using CommerceHub.Api.Dtos;
using CommerceHub.Api.Repositories;

namespace CommerceHub.Api.Services;

public sealed class ProductsService
{
    private readonly IProductsRepository _products;

    public ProductsService(IProductsRepository products)
    {
        _products = products;
    }

    public async Task<(bool Success, string? Error)> AdjustStockAsync(string id, AdjustStockRequest req, CancellationToken ct)
    {
        if (req is null) return (false, "Request body is required.");

        var ok = await _products.TryAdjustStockAsync(id, req.Delta, ct);
        return ok ? (true, null) : (false, "Insufficient stock or product not found.");
    }
}