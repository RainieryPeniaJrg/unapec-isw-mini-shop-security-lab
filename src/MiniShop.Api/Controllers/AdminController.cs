using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniShop.Api.Services;

namespace MiniShop.Api.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminController(IAdminService administration) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await administration.Admin(ct));
}
