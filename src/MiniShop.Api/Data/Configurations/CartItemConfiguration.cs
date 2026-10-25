using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniShop.Api.Models;

namespace MiniShop.Api.Data.Configurations;

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.CartId, item.ProductId }).IsUnique();
        builder.HasOne<Cart>().WithMany().HasForeignKey(item => item.CartId);
        builder.HasOne<Product>().WithMany().HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
