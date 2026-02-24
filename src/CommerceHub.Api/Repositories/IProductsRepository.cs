using CommerceHub.Api.Domain.Products;

namespace CommerceHub.Api.Repositories;

public interface IProductsRepository
{
    Task<Product?> GetByIdAsync(string id, CancellationToken ct);
    Task<bool> TryDecrementStockAsync(string productId, int qty, CancellationToken ct);
    Task<bool> TryAdjustStockAsync(string productId, int delta, CancellationToken ct);
    Task<bool> ForceIncrementStockAsync(string productId, int delta, CancellationToken ct);
}