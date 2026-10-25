using Microsoft.EntityFrameworkCore;
using MiniShop.Api.Data;
using MiniShop.Api.Models;

namespace MiniShop.Api.Services;

internal sealed class AdminService(ShopDbContext db, ICatalogService catalog) : IAdminService
{
    public async Task<AdminResponse> Admin(CancellationToken ct)
    {
        var users = await db.Users.Select(x => new UserResponse(x.Id, x.Username, x.Role)).ToListAsync(ct);
        var products = await catalog.Products(null, ct);
        var orders = await db.Orders.Include(x => x.Items).OrderBy(x => x.Id).ToListAsync(ct);

        return new AdminResponse(users, products, orders.Select(OrderMapping.ToResponse).ToList());
    }
}
