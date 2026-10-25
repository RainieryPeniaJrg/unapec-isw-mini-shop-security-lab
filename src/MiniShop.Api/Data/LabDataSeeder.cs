using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiniShop.Api.Models;

namespace MiniShop.Api.Data;

public sealed class LabDataSeeder(ShopDbContext db, IPasswordHasher<User> passwordHasher) : ILabDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        await SeedUsersAsync(cancellationToken);
        await SeedCatalogueAndCartsAsync(cancellationToken);
        await SeedCustomerResourcesAsync(cancellationToken);
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        var accounts = new[]
        {
            ("cliente-a", "LabClienteA2026!", "Cliente"),
            ("cliente-b", "LabClienteB2026!", "Cliente"),
            ("cliente-xss", "LabXss2026!", "Cliente"),
            ("cliente-brute", "LabPass2026!", "Cliente"),
            ("vendedor-lab", "LabSeller2026!", "Vendedor"),
            ("admin-lab", "LabAdmin2026!", "Admin")
        };

        foreach (var (username, password, role) in accounts)
        {
            var user = new User { Username = username, Role = role, PasswordHash = "" };
            user.PasswordHash = passwordHasher.HashPassword(user, password);
            db.Users.Add(user);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedCatalogueAndCartsAsync(CancellationToken cancellationToken)
    {
        db.Products.AddRange(
            new Product { Name = "Teclado demo", Description = "Teclado ficticio de laboratorio.", PriceCents = 250000, SellerId = 5 },
            new Product { Name = "Ratón demo", Description = "Ratón para prácticas académicas.", PriceCents = 95000, SellerId = 5 },
            new Product { Name = "Cuaderno de pruebas", Description = "Producto sintético para búsqueda.", PriceCents = 15000, SellerId = 5 });
        db.Carts.AddRange(Enumerable.Range(1, 4).Select(userId => new Cart { UserId = userId }));

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedCustomerResourcesAsync(CancellationToken cancellationToken)
    {
        db.CartItems.AddRange(
            new CartItem { CartId = 1, UserId = 1, ProductId = 1, Quantity = 1 },
            new CartItem { CartId = 2, UserId = 2, ProductId = 2, Quantity = 1 });
        db.Comments.Add(new ProductComment { ProductId = 1, UserId = 2, Body = "Comentario sintético de bienvenida." });
        db.Orders.AddRange(
            new Order { UserId = 1, Items = [new OrderItem { ProductId = 1, Name = "Teclado demo", PriceCents = 250000, Quantity = 1 }] },
            new Order { UserId = 2, Items = [new OrderItem { ProductId = 2, Name = "Ratón demo", PriceCents = 95000, Quantity = 1 }] });

        await db.SaveChangesAsync(cancellationToken);
    }
}
