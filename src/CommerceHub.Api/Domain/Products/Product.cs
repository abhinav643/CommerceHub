using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CommerceHub.Api.Domain.Products;

public sealed class Product
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    public string Sku { get; set; } = default!;
    public string Name { get; set; } = default!;

    // Must never go negative
    public int Stock { get; set; }

    public decimal Price { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}