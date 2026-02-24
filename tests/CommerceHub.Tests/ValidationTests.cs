using CommerceHub.Api.Dtos;
using CommerceHub.Api.Infrastructure.Messaging;
using CommerceHub.Api.Repositories;
using CommerceHub.Api.Services;
using Moq;
using NUnit.Framework;

namespace CommerceHub.Tests;

public sealed class ValidationTests
{
    [Test]
    public async Task Checkout_NegativeQuantity_FailsValidation()
    {
        var products = new Mock<IProductsRepository>(MockBehavior.Strict);
        var orders = new Mock<IOrdersRepository>(MockBehavior.Strict);
        var publisher = new Mock<IRabbitPublisher>(MockBehavior.Strict);

        var svc = new CheckoutService(products.Object, orders.Object, publisher.Object);

        var req = new CheckoutRequest
        {
            CustomerId = "c1",
            Items = new()
            {
                new CheckoutItemDto { ProductId = "p1", Quantity = -1 }
            }
        };

        var (success, error, order) = await svc.CheckoutAsync(req, CancellationToken.None);

        Assert.That(success, Is.False);
        Assert.That(order, Is.Null);
        Assert.That(error, Does.Contain("Quantity"));
    }
}