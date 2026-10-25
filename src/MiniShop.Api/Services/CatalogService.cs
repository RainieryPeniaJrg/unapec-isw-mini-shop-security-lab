using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MiniShop.Api.Data;
using MiniShop.Api.Models;

namespace MiniShop.Api.Services;

internal sealed class CatalogService(
    ShopDbContext db,
    LabOptions lab,
    IRedTeamProductSearch redTeamSearch,
    ILogger<CatalogService> logger) : ICatalogService
{
    public async Task<List<ProductResponse>> Products(string? q, CancellationToken ct)
    {
        if (q is not null && lab.RedTeam)
        {
            return await redTeamSearch.Search(q, ct);
        }

        var query = db.Products.AsNoTracking();
        if (!string.IsNullOrEmpty(q))
        {
            query = query.Where(x => x.Name.Contains(q));
        }

        return await query.OrderBy(x => x.Id)
            .Select(x => new ProductResponse(x.Id, x.Name, x.Description, x.PriceCents, x.SellerId))
            .ToListAsync(ct);
    }

    public async Task<ProductResponse> Product(int id, CancellationToken ct) =>
        await db.Products.Where(x => x.Id == id)
            .Select(x => new ProductResponse(x.Id, x.Name, x.Description, x.PriceCents, x.SellerId))
            .SingleOrDefaultAsync(ct) ?? throw ShopErrors.Missing();

    public async Task<int> SaveProduct(int? id, ProductRequest request, ClaimsPrincipal actor, CancellationToken ct)
    {
        if (!actor.IsInRole("Vendedor") && !actor.IsInRole("Admin"))
        {
            throw ShopErrors.Denied();
        }

        var product = id.HasValue
            ? await db.Products.FindAsync([id.Value], ct) ?? throw ShopErrors.Missing()
            : new Product { Name = request.Name, SellerId = actor.UserId() };
        if (id.HasValue && !actor.IsInRole("Admin") && product.SellerId != actor.UserId())
        {
            throw ShopErrors.Denied();
        }

        product.Name = request.Name;
        product.Description = request.Description ?? "";
        product.PriceCents = request.PriceCents;
        if (!id.HasValue)
        {
            db.Products.Add(product);
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Product saved actor={Actor} product={Product}", actor.UserId(), product.Id);
        return product.Id;
    }

    public async Task DeleteProduct(int id, ClaimsPrincipal actor, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([id], ct) ?? throw ShopErrors.Missing();
        if (!actor.IsInRole("Admin") && (!actor.IsInRole("Vendedor") || product.SellerId != actor.UserId()))
        {
            throw ShopErrors.Denied();
        }

        if (await db.CartItems.AnyAsync(x => x.ProductId == id, ct))
        {
            throw new ShopException(409, "El producto está en un carrito; retírelo antes de eliminar.");
        }

        db.Products.Remove(product);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Product deleted actor={Actor} product={Product}", actor.UserId(), id);
    }
}
