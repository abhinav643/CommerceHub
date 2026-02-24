using CommerceHub.Api.Dtos;
using CommerceHub.Api.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace CommerceHub.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly CheckoutService _checkout;
    private readonly OrdersService _orders;

    public OrdersController(CheckoutService checkout, OrdersService orders)
    {
        _checkout = checkout;
        _orders = orders;
    }

    // POST /api/orders/checkout
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest req, CancellationToken ct)
    {
        var (success, error, order) = await _checkout.CheckoutAsync(req, ct);

        if (!success)
            return Conflict(new { message = error });

        // Return created order id so client can call GET /api/orders/{id}
        return CreatedAtAction(nameof(GetById), new { id = order!.Id }, order);
    }
    // GET /api/orders/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] string id, CancellationToken ct)
    {
        // Prevent 500s on bad ObjectId format
        if (!ObjectId.TryParse(id, out _))
            return BadRequest(new { message = "Invalid order id format. Expected a 24-character hex ObjectId." });

        var order = await _orders.GetByIdAsync(id, ct);
        if (order is null)
            return NotFound(new { message = $"Order {id} not found." });

        return Ok(order);
    }

    // PUT /api/orders/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Replace(
        [FromRoute] string id,
        [FromBody] ReplaceOrderRequest req,
        CancellationToken ct)
    {
        // Prevent 500s on bad ObjectId format
        if (!ObjectId.TryParse(id, out _))
            return BadRequest(new { message = "Invalid order id format. Expected a 24-character hex ObjectId." });

        var (success, error, notFound, shippedBlocked) = await _orders.ReplaceAsync(id, req, ct);

        if (!success)
        {
            if (notFound) return NotFound(new { message = error });
            if (shippedBlocked) return Conflict(new { message = error });
            return BadRequest(new { message = error });
        }

        return NoContent();
    }
}
