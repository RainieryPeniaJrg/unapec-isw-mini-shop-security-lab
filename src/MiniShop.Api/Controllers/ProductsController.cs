using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniShop.Api.Models;
using MiniShop.Api.Services;

namespace MiniShop.Api.Controllers;

public sealed class ProductsController(ICatalogService catalog, ICommentService comments) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? q, CancellationToken ct)
    {
        ViewBag.Query = q;
        return View(await catalog.Products(q, ct));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        ViewBag.Comments = await comments.Comments(id, ct);
        return View(await catalog.Product(id, ct));
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Comment(int id, CommentRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Escriba un comentario de hasta 4000 caracteres.");
        }

        await comments.Comment(id, request.Body, User, ct);
        return RedirectToAction("Details", new { id });
    }
}
