using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniShop.Api.Services;

namespace MiniShop.Api.Controllers;

[Authorize(Roles = "Cliente")]
public sealed class OrdersController(IOrderService orders) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await orders.MyOrders(User, ct));

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct) => View(await orders.Order(id, User, ct));
}
