using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MiniShop.Api.Data;
using MiniShop.Api.Models;

namespace MiniShop.Api.Services;

internal sealed class OrderService(
    ShopDbContext db,
    ICartService cartService,
    ICustomerResourceAccess access) : IOrderService
{
    public async Task<OrderResponse> Checkout(int cartId, ClaimsPrincipal actor, CancellationToken ct)
    {
        var cart = await cartService.Cart(cartId, actor, ct);
        access.CheckClientOwner(cart.UserId, actor, false);
        if (cart.Items.Count == 0)
        {
            throw new ShopException(400, "El carrito está vacío.");
        }

        var order = new Order
        {
            UserId = actor.UserId(),
            Items = cart.Items.Select(x => new OrderItem
            {
                ProductId = x.ProductId,
                Name = x.Name,
                PriceCents = x.PriceCents,
                Quantity = x.Quantity
            }).ToList()
        };
        db.Orders.Add(order);
        db.CartItems.RemoveRange(await db.CartItems.Where(x => x.CartId == cartId).ToListAsync(ct));
        await db.SaveChangesAsync(ct);
        return OrderMapping.ToResponse(order);
    }

    public async Task<OrderResponse> Order(int id, ClaimsPrincipal actor, CancellationToken ct)
    {
        var order = await db.Orders.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ShopErrors.Missing();
        access.CheckClientOwner(order.UserId, actor);
        return OrderMapping.ToResponse(order);
    }

    public async Task<List<OrderResponse>> MyOrders(ClaimsPrincipal actor, CancellationToken ct)
    {
        access.CheckClient(actor);
        var orders = await db.Orders.Include(x => x.Items)
            .Where(x => x.UserId == actor.UserId())
            .OrderByDescending(x => x.Id)
            .ToListAsync(ct);

        return orders.Select(OrderMapping.ToResponse).ToList();
    }

    public async Task<OrderResponse> ChangeOrder(int id, int quantity, ClaimsPrincipal actor, CancellationToken ct)
    {
        var order = await db.Orders.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ShopErrors.Missing();
        access.CheckClientOwner(order.UserId, actor);
        order.Items.First().Quantity = quantity;
        await db.SaveChangesAsync(ct);
        return OrderMapping.ToResponse(order);
    }
}
