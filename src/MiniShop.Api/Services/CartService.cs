using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MiniShop.Api.Data;
using MiniShop.Api.Models;

namespace MiniShop.Api.Services;

internal sealed class CartService(
    ShopDbContext db,
    ICatalogService catalog,
    ICustomerResourceAccess access) : ICartService
{
    public async Task<CartResponse> Cart(int? id, ClaimsPrincipal actor, CancellationToken ct)
    {
        access.CheckClient(actor);
        var actorId = actor.UserId();
        var carts = id.HasValue ? db.Carts.Where(x => x.Id == id.Value) : db.Carts.Where(x => x.UserId == actorId);
        var cart = await carts.SingleOrDefaultAsync(ct) ?? throw ShopErrors.Missing();
        access.CheckClientOwner(cart.UserId, actor);
        var lines = await db.CartItems.Where(x => x.CartId == cart.Id)
            .Join(db.Products, x => x.ProductId, p => p.Id, (x, p) => new { Item = x, Product = p })
            .OrderBy(x => x.Item.Id)
            .Select(x => new LineResponse(x.Item.Id, x.Product.Id, x.Product.Name, x.Product.PriceCents, x.Item.Quantity))
            .ToListAsync(ct);

        return new CartResponse(cart.Id, cart.UserId, lines, lines.Sum(x => (long)x.Quantity * x.PriceCents));
    }

    public async Task ChangeItem(int cartId, int? itemId, int productId, int quantity, bool delete, ClaimsPrincipal actor, CancellationToken ct)
    {
        var cart = await db.Carts.FindAsync([cartId], ct) ?? throw ShopErrors.Missing();
        access.CheckClientOwner(cart.UserId, actor);
        var item = itemId.HasValue
            ? await db.CartItems.SingleOrDefaultAsync(x => x.Id == itemId && x.CartId == cartId, ct) ?? throw ShopErrors.Missing()
            : await db.CartItems.SingleOrDefaultAsync(x => x.ProductId == productId && x.CartId == cartId, ct);

        if (delete)
        {
            db.CartItems.Remove(item!);
        }
        else if (item is not null)
        {
            item.Quantity = quantity;
        }
        else
        {
            await catalog.Product(productId, ct);
            db.CartItems.Add(new CartItem { CartId = cartId, UserId = cart.UserId, ProductId = productId, Quantity = quantity });
        }

        await db.SaveChangesAsync(ct);
    }
}
