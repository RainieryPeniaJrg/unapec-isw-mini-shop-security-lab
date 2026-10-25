using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniShop.Api.Models;
using MiniShop.Api.Services;

namespace MiniShop.Api.Controllers;

[ApiController, Authorize, Route("api/orders")]
public sealed class OrdersApiController(IOrderService orders) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Mine(CancellationToken ct) => Ok(await orders.MyOrders(User, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct) => Ok(await orders.Order(id, User, ct));

    [HttpPost]
    public async Task<IActionResult> Create(CheckoutRequest request, CancellationToken ct)
    {
        var order = await orders.Checkout(request.CartId, User, ct);
        return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, QuantityRequest request, CancellationToken ct) =>
        Ok(await orders.ChangeOrder(id, request.Quantity, User, ct));
}
