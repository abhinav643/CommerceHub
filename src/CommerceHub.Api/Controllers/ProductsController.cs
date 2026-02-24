using CommerceHub.Api.Dtos;
using CommerceHub.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CommerceHub.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly ProductsService _products;

    public ProductsController(ProductsService products)
    {
        _products = products;
    }

    // PATCH /api/products/{id}/stock
    [HttpPatch("{id}/stock")]
    public async Task<IActionResult> AdjustStock([FromRoute] string id, [FromBody] AdjustStockRequest req, CancellationToken ct)
    {
        var (success, error) = await _products.AdjustStockAsync(id, req, ct);
        if (!success) return Conflict(new { message = error });
        return NoContent();
    }
}