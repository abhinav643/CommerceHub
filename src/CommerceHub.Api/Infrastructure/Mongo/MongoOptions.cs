namespace CommerceHub.Api.Infrastructure.Mongo;

public class MongoOptions
{
    public string ConnectionString { get; set; } = default!;
    public string Database { get; set; } = default!;
}