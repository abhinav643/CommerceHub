using MongoDB.Bson.Serialization.Conventions;
using CommerceHub.Api.Infrastructure.Mongo;
using CommerceHub.Api.Infrastructure.Messaging;
using CommerceHub.Api.Repositories;
using CommerceHub.Api.Services;

var pack = new ConventionPack
{
    new CamelCaseElementNameConvention()
};
ConventionRegistry.Register("camelCase", pack, _ => true);
var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuration binding
builder.Services.Configure<MongoOptions>(
    builder.Configuration.GetSection("Mongo"));

builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMq"));

// Infrastructure
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddSingleton<IRabbitPublisher, RabbitPublisher>();

// Repositories
builder.Services.AddScoped<IProductsRepository, ProductsRepository>();
builder.Services.AddScoped<IOrdersRepository, OrdersRepository>();

// Services
builder.Services.AddScoped<CheckoutService>();
builder.Services.AddScoped<OrdersService>();
builder.Services.AddScoped<ProductsService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();