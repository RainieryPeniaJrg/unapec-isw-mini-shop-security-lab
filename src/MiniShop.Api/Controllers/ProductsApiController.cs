using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniShop.Api.Models;
using MiniShop.Api.Services;

namespace MiniShop.Api.Controllers;

[ApiController, Route("api/products")]
public sealed class ProductsApiController(ICatalogService catalog, ICommentService comments) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await catalog.Products(null, ct));

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, CancellationToken ct) =>
        Ok(await catalog.Products(q, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct) => Ok(await catalog.Product(id, ct));

    [Authorize, HttpPost]
    public async Task<IActionResult> Create(ProductRequest request, CancellationToken ct)
    {
        var id = await catalog.SaveProduct(null, request, User, ct);
        return CreatedAtAction(nameof(Get), new { id }, await catalog.Product(id, ct));
    }

    [Authorize, HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ProductRequest request, CancellationToken ct)
    {
        await catalog.SaveProduct(id, request, User, ct);
        return Ok(await catalog.Product(id, ct));
    }

    [Authorize, HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await catalog.DeleteProduct(id, User, ct);
        return NoContent();
    }

    [HttpGet("{id:int}/comments")]
    public async Task<IActionResult> Comments(int id, CancellationToken ct) => Ok(await comments.Comments(id, ct));

    [Authorize, HttpPost("{id:int}/comments")]
    public async Task<IActionResult> Comment(int id, CommentRequest request, CancellationToken ct)
    {
        var commentId = await comments.Comment(id, request.Body, User, ct);
        return Created($"/api/products/{id}/comments", new { id = commentId });
    }
}
