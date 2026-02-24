using Microsoft.Extensions.Options;
using MongoDB.Driver;
using CommerceHub.Api.Domain.Products;
using CommerceHub.Api.Domain.Orders;

namespace CommerceHub.Api.Infrastructure.Mongo;

public class MongoDbContext
{
    public IMongoDatabase Database { get; }

    public IMongoCollection<Product> Products =>
        Database.GetCollection<Product>("Products");

    public IMongoCollection<Order> Orders =>
        Database.GetCollection<Order>("Orders");

    public MongoDbContext(IOptions<MongoOptions> options)
    {
        var client = new MongoClient(options.Value.ConnectionString);
        Database = client.GetDatabase(options.Value.Database);
    }
}