using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniShop.Api.Models;
using MiniShop.Api.Services;

namespace MiniShop.Api.Controllers;

[Authorize(Roles = "Vendedor,Admin")]
public sealed class SellerController(ICatalogService catalog) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Products(CancellationToken ct) => View(
        (await catalog.Products(null, ct))
            .Where(product => User.IsInRole("Admin") || product.SellerId == User.UserId())
            .ToList());

    [HttpGet]
    public async Task<IActionResult> Edit(int? id, CancellationToken ct)
    {
        var product = id.HasValue
            ? await catalog.Product(id.Value, ct)
            : new ProductResponse(0, "", "", 100, User.UserId());

        if (!User.IsInRole("Admin") && product.SellerId != User.UserId())
        {
            return Forbid();
        }

        return View(product);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int? id, ProductRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View("Edit", new ProductResponse(
                id ?? 0, request.Name, request.Description ?? "", request.PriceCents, User.UserId()));
        }

        await catalog.SaveProduct(id is > 0 ? id : null, request, User, ct);
        return RedirectToAction("Products");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await catalog.DeleteProduct(id, User, ct);
        return RedirectToAction("Products");
    }
}
