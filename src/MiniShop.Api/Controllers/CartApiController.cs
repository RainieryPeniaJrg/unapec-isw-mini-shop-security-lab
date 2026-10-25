using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniShop.Api.Models;
using MiniShop.Api.Services;

namespace MiniShop.Api.Controllers;

[ApiController, Authorize, Route("api/cart")]
public sealed class CartApiController(ICartService carts) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Mine(CancellationToken ct) => Ok(await carts.Cart(null, User, ct));

    [HttpGet("{cartId:int}")]
    public async Task<IActionResult> Get(int cartId, CancellationToken ct) => Ok(await carts.Cart(cartId, User, ct));

    [HttpPost("{cartId:int}/items")]
    public async Task<IActionResult> Add(int cartId, AddItemRequest request, CancellationToken ct)
    {
        await carts.ChangeItem(cartId, null, request.ProductId, request.Quantity, false, User, ct);
        return Created($"/api/cart/{cartId}", await carts.Cart(cartId, User, ct));
    }

    [HttpPut("{cartId:int}/items/{itemId:int}")]
    public async Task<IActionResult> Update(int cartId, int itemId, QuantityRequest request, CancellationToken ct)
    {
        await carts.ChangeItem(cartId, itemId, 0, request.Quantity, false, User, ct);
        return Ok(await carts.Cart(cartId, User, ct));
    }

    [HttpDelete("{cartId:int}/items/{itemId:int}")]
    public async Task<IActionResult> Delete(int cartId, int itemId, CancellationToken ct)
    {
        await carts.ChangeItem(cartId, itemId, 0, 0, true, User, ct);
        return NoContent();
    }
}
