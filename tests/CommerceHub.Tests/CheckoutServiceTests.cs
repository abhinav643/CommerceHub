using CommerceHub.Api.Domain.Products;
using CommerceHub.Api.Dtos;
using CommerceHub.Api.Infrastructure.Messaging;
using CommerceHub.Api.Infrastructure.Messaging.Events;
using CommerceHub.Api.Repositories;
using CommerceHub.Api.Services;
using Moq;
using NUnit.Framework;

namespace CommerceHub.Tests;

public sealed class CheckoutServiceTests
{
    private sealed class InMemoryProductsRepo : IProductsRepository
    {
        private readonly Dictionary<string, Product> _products;

        public InMemoryProductsRepo(IEnumerable<Product> seed)
        {
            _products = seed.ToDictionary(p => p.Id, p => p);
        }

        public Task<Product?> GetByIdAsync(string id, CancellationToken ct)
        {
            _products.TryGetValue(id, out var p);
            return Task.FromResult(p);
        }

        public Task<bool> TryDecrementStockAsync(string productId, int qty, CancellationToken ct)
        {
            if (!_products.TryGetValue(productId, out var p)) return Task.FromResult(false);
            if (qty <= 0) return Task.FromResult(false);
            if (p.Stock < qty) return Task.FromResult(false);

            p.Stock -= qty;
            return Task.FromResult(true);
        }

        public Task<bool> TryAdjustStockAsync(string productId, int delta, CancellationToken ct)
        {
            if (!_products.TryGetValue(productId, out var p)) return Task.FromResult(false);
            if (delta < 0 && p.Stock < -delta) return Task.FromResult(false);

            p.Stock += delta;
            return Task.FromResult(true);
        }

        public Task<bool> ForceIncrementStockAsync(string productId, int delta, CancellationToken ct)
        {
            if (!_products.TryGetValue(productId, out var p)) return Task.FromResult(false);
            if (delta <= 0) return Task.FromResult(false);

            p.Stock += delta;
            return Task.FromResult(true);
        }
    }

    private sealed class InMemoryOrdersRepo : IOrdersRepository
    {
        public Task<Api.Domain.Orders.Order?> GetByIdAsync(string id, CancellationToken ct)
            => Task.FromResult<Api.Domain.Orders.Order?>(null);

        public Task InsertAsync(Api.Domain.Orders.Order order, CancellationToken ct)
        {
            // simulate Mongo assigning an ID
            order.Id = Guid.NewGuid().ToString("N");
            return Task.CompletedTask;
        }

        public Task<bool> ReplaceIfNotShippedAsync(string id, Api.Domain.Orders.Order replacement, CancellationToken ct)
            => Task.FromResult(false);
    }

    [Test]
    public async Task Checkout_Success_DecrementsStock_AndPublishesEvent()
    {
        var prod1 = new Product { Id = "p1", Stock = 5, Price = 10m, Name = "A", Sku = "SKU1" };
        var prod2 = new Product { Id = "p2", Stock = 3, Price = 7m, Name = "B", Sku = "SKU2" };

        var products = new InMemoryProductsRepo(new[] { prod1, prod2 });
        var orders = new InMemoryOrdersRepo();

        var publisher = new Mock<IRabbitPublisher>(MockBehavior.Strict);
        publisher
            .Setup(p => p.PublishOrderCreatedAsync(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var svc = new CheckoutService(products, orders, publisher.Object);

        var req = new CheckoutRequest
        {
            CustomerId = "c1",
            Items = new()
            {
                new CheckoutItemDto { ProductId = "p1", Quantity = 2 },
                new CheckoutItemDto { ProductId = "p2", Quantity = 1 }
            }
        };

        var (success, error, order) = await svc.CheckoutAsync(req, CancellationToken.None);

        Assert.That(success, Is.True, error);
        Assert.That(order, Is.Not.Null);

        Assert.That(prod1.Stock, Is.EqualTo(3));
        Assert.That(prod2.Stock, Is.EqualTo(2));
        Assert.That(order!.Total, Is.EqualTo(27m));

        publisher.Verify(p => p.PublishOrderCreatedAsync(
            It.Is<OrderCreatedEvent>(e => e.CustomerId == "c1" && e.Total == 27m),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Checkout_InsufficientStock_RollsBack_AndDoesNotPublish()
    {
        var prod1 = new Product { Id = "p1", Stock = 5, Price = 10m, Name = "A", Sku = "SKU1" };
        var prod2 = new Product { Id = "p2", Stock = 0, Price = 7m, Name = "B", Sku = "SKU2" };

        var products = new InMemoryProductsRepo(new[] { prod1, prod2 });
        var orders = new InMemoryOrdersRepo();

        var publisher = new Mock<IRabbitPublisher>(MockBehavior.Strict);

        var svc = new CheckoutService(products, orders, publisher.Object);

        var req = new CheckoutRequest
        {
            CustomerId = "c1",
            Items = new()
            {
                new CheckoutItemDto { ProductId = "p1", Quantity = 2 }, // decrements first
                new CheckoutItemDto { ProductId = "p2", Quantity = 1 }  // fails
            }
        };

        var (success, error, order) = await svc.CheckoutAsync(req, CancellationToken.None);

        Assert.That(success, Is.False);
        Assert.That(order, Is.Null);
        Assert.That(error, Does.Contain("Insufficient stock"));

        // rollback happened
        Assert.That(prod1.Stock, Is.EqualTo(5));

        publisher.Verify(p => p.PublishOrderCreatedAsync(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}