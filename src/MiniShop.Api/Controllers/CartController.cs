using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniShop.Api.Models;
using MiniShop.Api.Services;

namespace MiniShop.Api.Controllers;

[Authorize(Roles = "Cliente")]
public sealed class CartController(ICartService carts, IOrderService orders) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await carts.Cart(null, User, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(AddItemRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Cantidad inválida.");
        }

        var cart = await carts.Cart(null, User, ct);
        await carts.ChangeItem(cart.Id, null, request.ProductId, request.Quantity, false, User, ct);
        return RedirectToAction("Index");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int cartId, int itemId, QuantityRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Cantidad inválida.");
        }

        await carts.ChangeItem(cartId, itemId, 0, request.Quantity, false, User, ct);
        return RedirectToAction("Index");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int cartId, int itemId, CancellationToken ct)
    {
        await carts.ChangeItem(cartId, itemId, 0, 0, true, User, ct);
        return RedirectToAction("Index");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(int cartId, CancellationToken ct)
    {
        var order = await orders.Checkout(cartId, User, ct);
        return RedirectToAction("Details", "Orders", new { id = order.Id });
    }
}
