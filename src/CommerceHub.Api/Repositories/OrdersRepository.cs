using CommerceHub.Api.Domain.Orders;
using CommerceHub.Api.Infrastructure.Mongo;
using MongoDB.Driver;

namespace CommerceHub.Api.Repositories;

public sealed class OrdersRepository : IOrdersRepository
{
    private readonly MongoDbContext _db;

    public OrdersRepository(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<Order?> GetByIdAsync(string id, CancellationToken ct)
    {
        var filter = Builders<Order>.Filter.Eq(o => o.Id, id);
        return await _db.Orders.Find(filter).FirstOrDefaultAsync(ct);
    }

    public Task InsertAsync(Order order, CancellationToken ct)
        => _db.Orders.InsertOneAsync(order, cancellationToken: ct);

    public async Task<bool> ReplaceIfNotShippedAsync(string id, Order replacement, CancellationToken ct)
    {
        var filter = Builders<Order>.Filter.And(
            Builders<Order>.Filter.Eq(o => o.Id, id),
            Builders<Order>.Filter.Ne(o => o.Status, OrderStatus.Shipped)
        );

        replacement.Id = id;
        replacement.UpdatedAtUtc = DateTime.UtcNow;

        var result = await _db.Orders.ReplaceOneAsync(filter, replacement, cancellationToken: ct);
        return result.ModifiedCount == 1;
    }
}