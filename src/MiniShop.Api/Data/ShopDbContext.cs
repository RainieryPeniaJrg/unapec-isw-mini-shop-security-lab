using Microsoft.EntityFrameworkCore;
using MiniShop.Api.Models;

namespace MiniShop.Api.Data;

public sealed class ShopDbContext(DbContextOptions<ShopDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<ProductComment> Comments => Set<ProductComment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(x => x.Username).IsUnique();
        modelBuilder.Entity<User>().Property(x => x.Role).HasMaxLength(20);
        modelBuilder.Entity<Product>().Property(x => x.Name).HasMaxLength(120);
        modelBuilder.Entity<Product>().Property(x => x.PriceCents).IsRequired();
        modelBuilder.Entity<CartItem>().HasIndex(x => new { x.UserId, x.ProductId }).IsUnique();
    }
}
