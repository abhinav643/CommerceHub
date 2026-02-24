using CommerceHub.Api.Domain.Products;
using CommerceHub.Api.Infrastructure.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CommerceHub.Api.Repositories;

public sealed class ProductsRepository : IProductsRepository
{
    private readonly MongoDbContext _db;

    public ProductsRepository(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<Product?> GetByIdAsync(string id, CancellationToken ct)
    {
        if (!ObjectId.TryParse(id, out var oid)) return null;

        var filter = Builders<Product>.Filter.Eq("_id", oid);
        return await _db.Products.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task<bool> TryDecrementStockAsync(string productId, int qty, CancellationToken ct)
    {
        if (qty <= 0) return false;
        if (!ObjectId.TryParse(productId, out var oid)) return false;

        var filter = Builders<Product>.Filter.And(
            Builders<Product>.Filter.Eq("_id", oid),
            Builders<Product>.Filter.Gte(p => p.Stock, qty)
        );

        var update = Builders<Product>.Update
            .Inc(p => p.Stock, -qty)
            .Set(p => p.UpdatedAtUtc, DateTime.UtcNow);

        var result = await _db.Products.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount == 1;
    }

    public async Task<bool> TryAdjustStockAsync(string productId, int delta, CancellationToken ct)
    {
        if (!ObjectId.TryParse(productId, out var oid)) return false;

        FilterDefinition<Product> filter;

        if (delta < 0)
        {
            var required = -delta;
            filter = Builders<Product>.Filter.And(
                Builders<Product>.Filter.Eq("_id", oid),
                Builders<Product>.Filter.Gte(p => p.Stock, required)
            );
        }
        else
        {
            filter = Builders<Product>.Filter.Eq("_id", oid);
        }

        var update = Builders<Product>.Update
            .Inc(p => p.Stock, delta)
            .Set(p => p.UpdatedAtUtc, DateTime.UtcNow);

        var result = await _db.Products.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount == 1;
    }

    public async Task<bool> ForceIncrementStockAsync(string productId, int delta, CancellationToken ct)
    {
        if (delta <= 0) return false;
        if (!ObjectId.TryParse(productId, out var oid)) return false;

        var filter = Builders<Product>.Filter.Eq("_id", oid);

        var update = Builders<Product>.Update
            .Inc(p => p.Stock, delta)
            .Set(p => p.UpdatedAtUtc, DateTime.UtcNow);

        var result = await _db.Products.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount == 1;
    }
}